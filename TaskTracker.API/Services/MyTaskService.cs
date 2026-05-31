using Microsoft.EntityFrameworkCore;
using TaskTracker.API.Data;
using TaskTracker.API.DTOs;
using TaskTracker.API.Interfaces;
using TaskTracker.API.Models;

namespace TaskTracker.API.Services;

public sealed class MyTaskService(AppDbContext dbContext) : IMyTaskService
{
    private const string Assigned = "ASSIGNED";
    private const string OnProgress = "ON_PROGRESS";
    private const string NeedReview = "NEED_REVIEW";
    private const string NeedRevision = "NEED_REVISION";
    private const string Cleared = "CLEARED";

    private static readonly IReadOnlyDictionary<string, string[]> AssigneeTransitions = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        [Assigned] = [OnProgress],
        [OnProgress] = [NeedReview],
        [NeedRevision] = [OnProgress]
    };

    private static readonly IReadOnlyDictionary<string, string[]> ReviewerTransitions = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        [NeedReview] = [NeedRevision, Cleared]
    };

    public async Task<IEnumerable<TaskResponse>> GetMyTasksAsync(
        int currentUserId,
        int? statusId,
        string? search,
        string? sortBy,
        CancellationToken cancellationToken)
    {
        var query = GetMyTasksQuery(currentUserId);

        if (statusId.HasValue)
            query = query.Where(task => task.TaskStatusId == statusId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLower();
            query = query.Where(task => task.TaskTitle.ToLower().Contains(normalizedSearch)
                || (task.TaskDescription != null && task.TaskDescription.ToLower().Contains(normalizedSearch)));
        }

        query = ApplySorting(query, sortBy);

        var tasks = await query.AsNoTracking().ToListAsync(cancellationToken);
        return await MapTaskSummariesAsync(tasks, cancellationToken);
    }

    public async Task<TaskResponse> GetMyTaskByIdAsync(
        int currentUserId,
        int taskId,
        CancellationToken cancellationToken)
    {
        var task = await GetMyTasksQuery(currentUserId)
            .AsNoTracking()
            .FirstOrDefaultAsync(task => task.TaskId == taskId, cancellationToken);

        if (task is null)
            throw new KeyNotFoundException($"Task with ID {taskId} was not found or is not accessible.");

        return await MapTaskDetailAsync(task, cancellationToken);
    }

    public async Task<TaskResponse> UpdateMyTaskStatusAsync(
        int currentUserId,
        int taskId,
        UpdateMyTaskStatusRequest request,
        CancellationToken cancellationToken)
    {
        var task = await GetMyTasksQuery(currentUserId)
            .FirstOrDefaultAsync(task => task.TaskId == taskId, cancellationToken);

        if (task is null)
            throw new KeyNotFoundException($"Task with ID {taskId} was not found or is not accessible.");

        var statuses = await dbContext.MstTaskStatuses
            .Where(status => status.DeletedDate == null)
            .ToDictionaryAsync(status => status.TaskStatusId, status => status.StatusName, cancellationToken);

        if (!statuses.TryGetValue(task.TaskStatusId, out var currentStatusName))
            throw new InvalidOperationException("Current task status is invalid.");

        if (!statuses.TryGetValue(request.TaskStatusId, out var nextStatusName))
            throw new InvalidOperationException("Requested task status is invalid.");

        var currentStatus = NormalizeStatusName(currentStatusName);
        var nextStatus = NormalizeStatusName(nextStatusName);
        var actor = ResolveActor(task, currentUserId, currentStatus, nextStatus);

        ValidateTransition(currentStatus, nextStatus, actor);

        if (task.TaskStatusId == request.TaskStatusId)
            return await MapTaskDetailAsync(task, cancellationToken);

        var oldStatusId = task.TaskStatusId;
        task.TaskStatusId = request.TaskStatusId;
        task.UpdatedDate = DateTime.UtcNow;
        task.UpdatedBy = currentUserId.ToString();

        dbContext.TrxTaskHistories.Add(new TrxTaskHistory
        {
            TaskId = task.TaskId,
            OldTaskStatusId = oldStatusId,
            NewTaskStatusId = task.TaskStatusId,
            ActionType = "STATUS_CHANGE",
            Remarks = string.IsNullOrWhiteSpace(request.Remarks)
                ? $"Status changed from {currentStatusName} to {nextStatusName}."
                : request.Remarks.Trim(),
            CreatedDate = DateTime.UtcNow,
            CreatedBy = currentUserId.ToString()
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetMyTaskByIdAsync(currentUserId, task.TaskId, cancellationToken);
    }

    public async Task<TaskCommentResponse> AddMyTaskCommentAsync(
        int currentUserId,
        int taskId,
        AddMyTaskCommentRequest request,
        CancellationToken cancellationToken)
    {
        var isAccessible = await GetMyTasksQuery(currentUserId)
            .AnyAsync(task => task.TaskId == taskId, cancellationToken);

        if (!isAccessible)
            throw new KeyNotFoundException($"Task with ID {taskId} was not found or is not accessible.");

        var now = DateTime.UtcNow;
        var comment = new TrxTaskComment
        {
            TaskId = taskId,
            ReviewerUserId = currentUserId,
            CommentText = request.CommentText.Trim(),
            CreatedDate = now,
            CreatedBy = currentUserId.ToString()
        };

        dbContext.TrxTaskComments.Add(comment);
        await dbContext.SaveChangesAsync(cancellationToken);

        var userName = await GetUserNameAsync(currentUserId, cancellationToken);

        return new TaskCommentResponse(
            TaskCommentId: comment.TaskCommentId,
            ReviewerUserId: comment.ReviewerUserId,
            ReviewerName: userName,
            CommentText: comment.CommentText,
            CreatedDate: comment.CreatedDate,
            CreatedBy: comment.CreatedBy);
    }

    private IQueryable<TrxTask> GetMyTasksQuery(int currentUserId)
    {
        return dbContext.TrxTasks.Where(task => task.DeletedDate == null
            && (task.AssigneeUserId == currentUserId || task.ReviewerUserId == currentUserId));
    }

    private static IQueryable<TrxTask> ApplySorting(IQueryable<TrxTask> query, string? sortBy)
    {
        return sortBy?.Trim().ToLowerInvariant() switch
        {
            "newest" => query.OrderByDescending(task => task.CreatedDate),
            "status" => query.OrderBy(task => task.TaskStatusId).ThenBy(task => task.DeadlineDate),
            _ => query.OrderBy(task => task.DeadlineDate).ThenByDescending(task => task.CreatedDate)
        };
    }

    private async Task<IReadOnlyList<TaskResponse>> MapTaskSummariesAsync(
        IReadOnlyCollection<TrxTask> tasks,
        CancellationToken cancellationToken)
    {
        if (tasks.Count == 0)
            return [];

        var users = await GetUsersDictionaryAsync(tasks, cancellationToken);
        var statuses = await GetStatusesDictionaryAsync(tasks.Select(task => task.TaskStatusId), cancellationToken);

        return tasks.Select(task => MapTaskResponse(
            task,
            users.GetValueOrDefault(task.AssigneeUserId, "Unknown User"),
            users.GetValueOrDefault(task.ReviewerUserId, "Unknown User"),
            statuses.GetValueOrDefault(task.TaskStatusId, "Unknown Status"),
            [],
            [],
            [])).ToList();
    }

    private async Task<TaskResponse> MapTaskDetailAsync(TrxTask task, CancellationToken cancellationToken)
    {
        var users = await GetUsersDictionaryAsync([task], cancellationToken);
        var historyResponses = await GetHistoryResponsesAsync(task.TaskId, cancellationToken);
        var commentResponses = await GetCommentResponsesAsync(task.TaskId, cancellationToken);
        var documentResponses = await GetDocumentResponsesAsync(task.TaskId, cancellationToken);
        var statusName = await GetStatusNameAsync(task.TaskStatusId, cancellationToken);

        return MapTaskResponse(
            task,
            users.GetValueOrDefault(task.AssigneeUserId, "Unknown User"),
            users.GetValueOrDefault(task.ReviewerUserId, "Unknown User"),
            statusName,
            historyResponses,
            commentResponses,
            documentResponses);
    }

    private static TaskResponse MapTaskResponse(
        TrxTask task,
        string assigneeName,
        string reviewerName,
        string statusName,
        IReadOnlyList<TaskHistoryResponse> history,
        IReadOnlyList<TaskCommentResponse> comments,
        IReadOnlyList<TaskDocumentResponse> documents)
    {
        return new TaskResponse(
            TaskId: task.TaskId,
            TaskTitle: task.TaskTitle,
            TaskDescription: task.TaskDescription,
            AssigneeUserId: task.AssigneeUserId,
            AssigneeName: assigneeName,
            ReviewerUserId: task.ReviewerUserId,
            ReviewerName: reviewerName,
            DeadlineDate: task.DeadlineDate,
            TaskStatusId: task.TaskStatusId,
            StatusName: statusName,
            CreatedDate: task.CreatedDate,
            CreatedBy: task.CreatedBy,
            UpdatedDate: task.UpdatedDate,
            UpdatedBy: task.UpdatedBy,
            IsOverdue: task.DeadlineDate.Date < DateTime.UtcNow.Date && !string.Equals(NormalizeStatusName(statusName), Cleared, StringComparison.OrdinalIgnoreCase),
            History: history,
            Comments: comments,
            Documents: documents);
    }

    private async Task<Dictionary<int, string>> GetUsersDictionaryAsync(
        IEnumerable<TrxTask> tasks,
        CancellationToken cancellationToken)
    {
        var userIds = tasks
            .SelectMany(task => new[] { task.AssigneeUserId, task.ReviewerUserId })
            .Distinct()
            .ToArray();

        return await dbContext.MstUsers
            .Where(user => userIds.Contains(user.UserId))
            .ToDictionaryAsync(user => user.UserId, user => user.FullName, cancellationToken);
    }

    private async Task<Dictionary<int, string>> GetStatusesDictionaryAsync(
        IEnumerable<int> statusIds,
        CancellationToken cancellationToken)
    {
        var ids = statusIds.Distinct().ToArray();

        return await dbContext.MstTaskStatuses
            .Where(status => ids.Contains(status.TaskStatusId))
            .ToDictionaryAsync(status => status.TaskStatusId, status => status.StatusName, cancellationToken);
    }

    private async Task<string> GetUserNameAsync(int userId, CancellationToken cancellationToken)
    {
        return await dbContext.MstUsers
            .Where(user => user.UserId == userId)
            .Select(user => user.FullName)
            .FirstOrDefaultAsync(cancellationToken) ?? "Unknown User";
    }

    private async Task<string> GetStatusNameAsync(int statusId, CancellationToken cancellationToken)
    {
        return await dbContext.MstTaskStatuses
            .Where(status => status.TaskStatusId == statusId)
            .Select(status => status.StatusName)
            .FirstOrDefaultAsync(cancellationToken) ?? "Unknown Status";
    }

    private async Task<IReadOnlyList<TaskHistoryResponse>> GetHistoryResponsesAsync(int taskId, CancellationToken cancellationToken)
    {
        var histories = await dbContext.TrxTaskHistories
            .Where(history => history.TaskId == taskId && history.DeletedDate == null)
            .OrderByDescending(history => history.CreatedDate)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (histories.Count == 0)
            return [];

        var statusIds = histories
            .SelectMany(history => new[] { history.OldTaskStatusId, history.NewTaskStatusId })
            .Where(statusId => statusId.HasValue)
            .Select(statusId => statusId!.Value);

        var statuses = await GetStatusesDictionaryAsync(statusIds, cancellationToken);

        return histories.Select(history => new TaskHistoryResponse(
            TaskHistoryId: history.TaskHistoryId,
            OldTaskStatusId: history.OldTaskStatusId,
            OldStatusName: history.OldTaskStatusId.HasValue ? statuses.GetValueOrDefault(history.OldTaskStatusId.Value) : null,
            NewTaskStatusId: history.NewTaskStatusId,
            NewStatusName: statuses.GetValueOrDefault(history.NewTaskStatusId, "Unknown Status"),
            ActionType: history.ActionType,
            Remarks: history.Remarks,
            CreatedDate: history.CreatedDate,
            CreatedBy: history.CreatedBy)).ToList();
    }

    private async Task<IReadOnlyList<TaskCommentResponse>> GetCommentResponsesAsync(int taskId, CancellationToken cancellationToken)
    {
        var comments = await dbContext.TrxTaskComments
            .Where(comment => comment.TaskId == taskId && comment.DeletedDate == null)
            .OrderByDescending(comment => comment.CreatedDate)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (comments.Count == 0)
            return [];

        var userIds = comments.Select(comment => comment.ReviewerUserId).Distinct().ToArray();
        var users = await dbContext.MstUsers
            .Where(user => userIds.Contains(user.UserId))
            .ToDictionaryAsync(user => user.UserId, user => user.FullName, cancellationToken);

        return comments.Select(comment => new TaskCommentResponse(
            TaskCommentId: comment.TaskCommentId,
            ReviewerUserId: comment.ReviewerUserId,
            ReviewerName: users.GetValueOrDefault(comment.ReviewerUserId, "Unknown User"),
            CommentText: comment.CommentText,
            CreatedDate: comment.CreatedDate,
            CreatedBy: comment.CreatedBy)).ToList();
    }

    private async Task<IReadOnlyList<TaskDocumentResponse>> GetDocumentResponsesAsync(int taskId, CancellationToken cancellationToken)
    {
        var documents = await dbContext.TrxTaskDocuments
            .Where(taskDocument => taskDocument.TaskId == taskId && taskDocument.DeletedDate == null)
            .Join(
                dbContext.MstDocuments.Where(document => document.DeletedDate == null),
                taskDocument => taskDocument.DocumentId,
                document => document.DocumentId,
                (taskDocument, document) => new { taskDocument, document })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return documents.Select(item => new TaskDocumentResponse(
            TaskDocumentId: item.taskDocument.TaskDocumentId,
            DocumentId: item.document.DocumentId,
            FileName: item.document.FileName,
            MimeType: item.document.MimeType,
            FileSize: item.document.FileSize,
            DocumentType: item.taskDocument.DocumentType,
            DocumentVersion: item.taskDocument.DocumentVersion,
            CreatedDate: item.taskDocument.CreatedDate,
            CreatedBy: item.taskDocument.CreatedBy)).ToList();
    }

    private static string ResolveActor(TrxTask task, int currentUserId, string currentStatus, string nextStatus)
    {
        var isAssignee = task.AssigneeUserId == currentUserId;
        var isReviewer = task.ReviewerUserId == currentUserId;

        if (isAssignee && IsAllowedTransition(AssigneeTransitions, currentStatus, nextStatus))
            return "Assignee";

        if (isReviewer && IsAllowedTransition(ReviewerTransitions, currentStatus, nextStatus))
            return "Reviewer";

        return isAssignee ? "Assignee" : "Reviewer";
    }

    private static void ValidateTransition(string currentStatus, string nextStatus, string actor)
    {
        var transitions = string.Equals(actor, "Assignee", StringComparison.OrdinalIgnoreCase)
            ? AssigneeTransitions
            : ReviewerTransitions;

        if (IsAllowedTransition(transitions, currentStatus, nextStatus))
            return;

        throw new InvalidOperationException($"Invalid status transition from '{currentStatus}' to '{nextStatus}' for {actor}.");
    }

    private static bool IsAllowedTransition(
        IReadOnlyDictionary<string, string[]> transitions,
        string currentStatus,
        string nextStatus)
    {
        return transitions.TryGetValue(currentStatus, out var allowedStatuses)
            && allowedStatuses.Contains(nextStatus, StringComparer.OrdinalIgnoreCase);
    }

    private static string NormalizeStatusName(string statusName)
    {
        return statusName.Trim().Replace(" ", "_").Replace("-", "_").ToUpperInvariant();
    }
}
