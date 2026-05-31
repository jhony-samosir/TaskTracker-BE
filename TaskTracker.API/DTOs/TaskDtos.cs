namespace TaskTracker.API.DTOs;

public sealed record TaskQueryParameters(int? AssigneeUserId, int? ReviewerUserId, int? StatusId, string? Search);

public sealed record CreateTaskRequest(
    string TaskTitle,
    string? TaskDescription,
    int AssigneeUserId,
    int ReviewerUserId,
    DateTime DeadlineDate,
    int TaskStatusId);

public sealed record UpdateTaskRequest(
    string TaskTitle,
    string? TaskDescription,
    int AssigneeUserId,
    int ReviewerUserId,
    DateTime DeadlineDate,
    int TaskStatusId);

public sealed record UpdateTaskStatusRequest(int TaskStatusId, string? Remarks);

public sealed record AddCommentRequest(string CommentText);

public sealed record AttachTaskDocumentRequest(
    string FileName,
    string MimeType,
    long? FileSize,
    string FileBase64,
    string DocumentType,
    int DocumentVersion = 1);

public sealed record TaskResponse(
    int TaskId,
    string TaskTitle,
    string? TaskDescription,
    int AssigneeUserId,
    string AssigneeName,
    int ReviewerUserId,
    string ReviewerName,
    DateTime DeadlineDate,
    int TaskStatusId,
    string StatusName,
    DateTime CreatedDate,
    string? CreatedBy,
    DateTime? UpdatedDate,
    string? UpdatedBy,
    bool IsOverdue,
    IReadOnlyList<TaskHistoryResponse> History,
    IReadOnlyList<TaskCommentResponse> Comments,
    IReadOnlyList<TaskDocumentResponse> Documents);

public sealed record TaskHistoryResponse(
    int TaskHistoryId,
    int? OldTaskStatusId,
    string? OldStatusName,
    int NewTaskStatusId,
    string NewStatusName,
    string ActionType,
    string? Remarks,
    DateTime CreatedDate,
    string? CreatedBy);

public sealed record TaskCommentResponse(
    int TaskCommentId,
    int ReviewerUserId,
    string ReviewerName,
    string CommentText,
    DateTime CreatedDate,
    string? CreatedBy);

public sealed record TaskDocumentResponse(
    int TaskDocumentId,
    int DocumentId,
    string FileName,
    string MimeType,
    long? FileSize,
    string DocumentType,
    int DocumentVersion,
    DateTime CreatedDate,
    string? CreatedBy);
