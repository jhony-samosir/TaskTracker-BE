using Microsoft.EntityFrameworkCore;
using TaskTracker.API.Data;
using TaskTracker.API.DTOs;
using TaskTracker.API.Interfaces;
using TaskTracker.API.Models;

namespace TaskTracker.API.Services;

public sealed class TaskService(AppDbContext dbContext) : ITaskService
{
    private async Task<string> GetUserNameAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.MstUsers.FindAsync(new object[] { userId }, cancellationToken);
        return user?.FullName ?? "Unknown User";
    }

    private async Task<string> GetStatusNameAsync(int statusId, CancellationToken cancellationToken)
    {
        var status = await dbContext.MstTaskStatuses.FindAsync(new object[] { statusId }, cancellationToken);
        return status?.StatusName ?? "Unknown Status";
    }

    private IQueryable<TrxTask> GetAccessibleTasksQuery(int currentUserId, string currentUserRole)
    {
        var query = dbContext.TrxTasks
            .Where(t => t.DeletedDate == null);

        if (!string.Equals(currentUserRole, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(t => t.AssigneeUserId == currentUserId || t.ReviewerUserId == currentUserId || t.CreatedBy == currentUserId.ToString());
        }

        return query;
    }

    public async Task<IEnumerable<TaskResponse>> GetTasksAsync(
        int? assigneeId,
        int? statusId,
        string? search,
        int currentUserId,
        string currentUserRole,
        CancellationToken cancellationToken)
    {
        var query = GetAccessibleTasksQuery(currentUserId, currentUserRole);

        if (assigneeId.HasValue)
            query = query.Where(t => t.AssigneeUserId == assigneeId.Value);

        if (statusId.HasValue)
            query = query.Where(t => t.TaskStatusId == statusId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(t => t.TaskTitle.ToLower().Contains(searchLower) ||
                                     (t.TaskDescription != null && t.TaskDescription.ToLower().Contains(searchLower)));
        }

        var tasks = await query.ToListAsync(cancellationToken);
        
        var userIds = tasks.Select(t => t.AssigneeUserId).Concat(tasks.Select(t => t.ReviewerUserId)).Distinct().ToList();
        var users = await dbContext.MstUsers.Where(u => userIds.Contains(u.UserId)).ToDictionaryAsync(u => u.UserId, u => u.FullName, cancellationToken);
        
        var statusIds = tasks.Select(t => t.TaskStatusId).Distinct().ToList();
        var statuses = await dbContext.MstTaskStatuses.Where(s => statusIds.Contains(s.TaskStatusId)).ToDictionaryAsync(s => s.TaskStatusId, s => s.StatusName, cancellationToken);

        return tasks.Select(t => new TaskResponse(
            TaskId: t.TaskId,
            TaskTitle: t.TaskTitle,
            TaskDescription: t.TaskDescription,
            AssigneeUserId: t.AssigneeUserId,
            AssigneeName: users.GetValueOrDefault(t.AssigneeUserId, "Unknown"),
            ReviewerUserId: t.ReviewerUserId,
            ReviewerName: users.GetValueOrDefault(t.ReviewerUserId, "Unknown"),
            DeadlineDate: t.DeadlineDate,
            TaskStatusId: t.TaskStatusId,
            StatusName: statuses.GetValueOrDefault(t.TaskStatusId, "Unknown"),
            CreatedDate: t.CreatedDate,
            CreatedBy: t.CreatedBy,
            UpdatedDate: t.UpdatedDate,
            UpdatedBy: t.UpdatedBy,
            IsOverdue: t.DeadlineDate.Date < DateTime.UtcNow.Date && statuses.GetValueOrDefault(t.TaskStatusId, "") != "Cleared",
            History: Array.Empty<TaskHistoryResponse>(),
            Comments: Array.Empty<TaskCommentResponse>(),
            Documents: Array.Empty<TaskDocumentResponse>()
        ));
    }

    public async Task<TaskResponse> GetTaskByIdAsync(
        int taskId,
        int currentUserId,
        string currentUserRole,
        CancellationToken cancellationToken)
    {
        var task = await GetAccessibleTasksQuery(currentUserId, currentUserRole)
            .FirstOrDefaultAsync(t => t.TaskId == taskId, cancellationToken);

        if (task is null)
            throw new KeyNotFoundException($"Task with ID {taskId} not found or you do not have permission to access it.");

        var assigneeName = await GetUserNameAsync(task.AssigneeUserId, cancellationToken);
        var reviewerName = await GetUserNameAsync(task.ReviewerUserId, cancellationToken);
        var statusName = await GetStatusNameAsync(task.TaskStatusId, cancellationToken);

        // Fetch Histories
        var histories = await dbContext.TrxTaskHistories
            .Where(h => h.TaskId == taskId && h.DeletedDate == null)
            .OrderByDescending(h => h.CreatedDate)
            .ToListAsync(cancellationToken);
            
        var allStatusIds = histories.Select(h => h.OldTaskStatusId ?? 0)
            .Concat(histories.Select(h => h.NewTaskStatusId))
            .Where(id => id > 0)
            .Distinct().ToList();
            
        var statusDict = await dbContext.MstTaskStatuses
            .Where(s => allStatusIds.Contains(s.TaskStatusId))
            .ToDictionaryAsync(s => s.TaskStatusId, s => s.StatusName, cancellationToken);

        var historyResponses = histories.Select(h => new TaskHistoryResponse(
            TaskHistoryId: h.TaskHistoryId,
            OldTaskStatusId: h.OldTaskStatusId,
            OldStatusName: h.OldTaskStatusId.HasValue ? statusDict.GetValueOrDefault(h.OldTaskStatusId.Value) : null,
            NewTaskStatusId: h.NewTaskStatusId,
            NewStatusName: statusDict.GetValueOrDefault(h.NewTaskStatusId, "Unknown"),
            ActionType: h.ActionType,
            Remarks: h.Remarks,
            CreatedDate: h.CreatedDate,
            CreatedBy: h.CreatedBy
        )).ToList();

        // Fetch Comments
        var comments = await dbContext.TrxTaskComments
            .Where(c => c.TaskId == taskId && c.DeletedDate == null)
            .OrderByDescending(c => c.CreatedDate)
            .ToListAsync(cancellationToken);

        var commentUserIds = comments.Select(c => c.ReviewerUserId).Distinct().ToList();
        var commentUsers = await dbContext.MstUsers
            .Where(u => commentUserIds.Contains(u.UserId))
            .ToDictionaryAsync(u => u.UserId, u => u.FullName, cancellationToken);

        var commentResponses = comments.Select(c => new TaskCommentResponse(
            TaskCommentId: c.TaskCommentId,
            ReviewerUserId: c.ReviewerUserId,
            ReviewerName: commentUsers.GetValueOrDefault(c.ReviewerUserId, "Unknown"),
            CommentText: c.CommentText,
            CreatedDate: c.CreatedDate,
            CreatedBy: c.CreatedBy
        )).ToList();
        
        // Fetch Documents
        var documents = await dbContext.TrxTaskDocuments
            .Where(d => d.TaskId == taskId && d.DeletedDate == null)
            .Join(dbContext.MstDocuments, 
                td => td.DocumentId, 
                md => md.DocumentId, 
                (td, md) => new { td, md })
            .ToListAsync(cancellationToken);

        var documentResponses = documents.Select(d => new TaskDocumentResponse(
            TaskDocumentId: d.td.TaskDocumentId,
            DocumentId: d.md.DocumentId,
            FileName: d.md.FileName,
            MimeType: d.md.MimeType,
            FileSize: d.md.FileSize,
            DocumentType: d.td.DocumentType,
            DocumentVersion: d.td.DocumentVersion,
            CreatedDate: d.td.CreatedDate,
            CreatedBy: d.td.CreatedBy
        )).ToList();

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
            IsOverdue: task.DeadlineDate.Date < DateTime.UtcNow.Date && statusName != "Cleared",
            History: historyResponses,
            Comments: commentResponses,
            Documents: documentResponses
        );
    }

    public async Task<TaskResponse> CreateTaskAsync(
        int currentUserId,
        CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var task = new TrxTask
        {
            TaskTitle = request.TaskTitle,
            TaskDescription = request.TaskDescription,
            AssigneeUserId = request.AssigneeUserId,
            ReviewerUserId = request.ReviewerUserId,
            DeadlineDate = request.DeadlineDate,
            TaskStatusId = request.TaskStatusId,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = currentUserId.ToString()
        };

        dbContext.TrxTasks.Add(task);
        await dbContext.SaveChangesAsync(cancellationToken);

        var history = new TrxTaskHistory
        {
            TaskId = task.TaskId,
            NewTaskStatusId = task.TaskStatusId,
            ActionType = "CREATE",
            Remarks = "Task created",
            CreatedDate = DateTime.UtcNow,
            CreatedBy = currentUserId.ToString()
        };

        dbContext.TrxTaskHistories.Add(history);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Fetch detail response (Admin access trick to make sure it loads)
        return await GetTaskByIdAsync(task.TaskId, currentUserId, "Admin", cancellationToken);
    }

    public async Task<TaskResponse> UpdateTaskAsync(
        int currentUserId,
        int taskId,
        UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.TrxTasks
            .FirstOrDefaultAsync(t => t.TaskId == taskId && t.DeletedDate == null, cancellationToken);

        if (task is null)
            throw new KeyNotFoundException($"Task with ID {taskId} not found.");

        var oldStatusId = task.TaskStatusId;

        task.TaskTitle = request.TaskTitle;
        task.TaskDescription = request.TaskDescription;
        task.AssigneeUserId = request.AssigneeUserId;
        task.ReviewerUserId = request.ReviewerUserId;
        task.DeadlineDate = request.DeadlineDate;
        task.TaskStatusId = request.TaskStatusId;
        task.UpdatedDate = DateTime.UtcNow;
        task.UpdatedBy = currentUserId.ToString();

        if (oldStatusId != request.TaskStatusId)
        {
            dbContext.TrxTaskHistories.Add(new TrxTaskHistory
            {
                TaskId = task.TaskId,
                OldTaskStatusId = oldStatusId,
                NewTaskStatusId = task.TaskStatusId,
                ActionType = "STATUS_UPDATE",
                Remarks = "Status updated during task modification",
                CreatedDate = DateTime.UtcNow,
                CreatedBy = currentUserId.ToString()
            });
        }
        else
        {
            dbContext.TrxTaskHistories.Add(new TrxTaskHistory
            {
                TaskId = task.TaskId,
                NewTaskStatusId = task.TaskStatusId,
                OldTaskStatusId = oldStatusId,
                ActionType = "UPDATE",
                Remarks = "Task details modified",
                CreatedDate = DateTime.UtcNow,
                CreatedBy = currentUserId.ToString()
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetTaskByIdAsync(task.TaskId, currentUserId, "Admin", cancellationToken);
    }

    public async Task<bool> DeleteTaskAsync(
        int currentUserId,
        int taskId,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.TrxTasks
            .FirstOrDefaultAsync(t => t.TaskId == taskId && t.DeletedDate == null, cancellationToken);

        if (task is null)
            throw new KeyNotFoundException($"Task with ID {taskId} not found.");

        task.DeletedDate = DateTime.UtcNow;
        task.DeletedBy = currentUserId.ToString();

        dbContext.TrxTaskHistories.Add(new TrxTaskHistory
        {
            TaskId = task.TaskId,
            OldTaskStatusId = task.TaskStatusId,
            NewTaskStatusId = task.TaskStatusId,
            ActionType = "DELETE",
            Remarks = "Task soft deleted",
            CreatedDate = DateTime.UtcNow,
            CreatedBy = currentUserId.ToString()
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<TaskResponse> UpdateTaskStatusAsync(
        int currentUserId,
        int taskId,
        UpdateTaskStatusRequest request,
        CancellationToken cancellationToken)
    {
        // Must fetch ignoring role, because users might only have access to their tasks anyway,
        // but here we validate that before updating.
        var task = await GetAccessibleTasksQuery(currentUserId, "Admin")
            .FirstOrDefaultAsync(t => t.TaskId == taskId, cancellationToken);

        if (task is null)
            throw new KeyNotFoundException($"Task with ID {taskId} not found.");

        // Additional role logic could be added here to enforce state machine per role

        var oldStatusId = task.TaskStatusId;
        task.TaskStatusId = request.TaskStatusId;
        task.UpdatedDate = DateTime.UtcNow;
        task.UpdatedBy = currentUserId.ToString();

        dbContext.TrxTaskHistories.Add(new TrxTaskHistory
        {
            TaskId = task.TaskId,
            OldTaskStatusId = oldStatusId,
            NewTaskStatusId = task.TaskStatusId,
            ActionType = "STATUS_UPDATE",
            Remarks = request.Remarks ?? "Status updated",
            CreatedDate = DateTime.UtcNow,
            CreatedBy = currentUserId.ToString()
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetTaskByIdAsync(task.TaskId, currentUserId, "Admin", cancellationToken);
    }

    public async Task<TaskCommentResponse> AddCommentAsync(
        int currentUserId,
        int taskId,
        AddCommentRequest request,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.TrxTasks
            .FirstOrDefaultAsync(t => t.TaskId == taskId && t.DeletedDate == null, cancellationToken);

        if (task is null)
            throw new KeyNotFoundException($"Task with ID {taskId} not found.");

        var comment = new TrxTaskComment
        {
            TaskId = taskId,
            ReviewerUserId = currentUserId,
            CommentText = request.CommentText,
            CreatedDate = DateTime.UtcNow,
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
            CreatedBy: comment.CreatedBy
        );
    }

    public async Task<TaskDocumentResponse> AttachDocumentAsync(
        int currentUserId,
        int taskId,
        AttachTaskDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.TrxTasks
            .FirstOrDefaultAsync(t => t.TaskId == taskId && t.DeletedDate == null, cancellationToken);

        if (task is null)
            throw new KeyNotFoundException($"Task with ID {taskId} not found.");

        var document = new MstDocument
        {
            FileName = request.FileName,
            MimeType = request.MimeType,
            FileSize = request.FileSize,
            FileBase64 = request.FileBase64,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = currentUserId.ToString()
        };

        dbContext.MstDocuments.Add(document);
        await dbContext.SaveChangesAsync(cancellationToken);

        var taskDocument = new TrxTaskDocument
        {
            TaskId = taskId,
            DocumentId = document.DocumentId,
            DocumentType = request.DocumentType,
            DocumentVersion = request.DocumentVersion,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = currentUserId.ToString()
        };

        dbContext.TrxTaskDocuments.Add(taskDocument);
        
        dbContext.TrxTaskHistories.Add(new TrxTaskHistory
        {
            TaskId = task.TaskId,
            OldTaskStatusId = task.TaskStatusId,
            NewTaskStatusId = task.TaskStatusId,
            ActionType = "DOCUMENT_UPLOAD",
            Remarks = $"Uploaded document: {request.FileName}",
            CreatedDate = DateTime.UtcNow,
            CreatedBy = currentUserId.ToString()
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new TaskDocumentResponse(
            TaskDocumentId: taskDocument.TaskDocumentId,
            DocumentId: document.DocumentId,
            FileName: document.FileName,
            MimeType: document.MimeType,
            FileSize: document.FileSize,
            DocumentType: taskDocument.DocumentType,
            DocumentVersion: taskDocument.DocumentVersion,
            CreatedDate: taskDocument.CreatedDate,
            CreatedBy: taskDocument.CreatedBy
        );
    }
}
