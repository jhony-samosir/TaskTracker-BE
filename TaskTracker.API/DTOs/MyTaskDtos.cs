namespace TaskTracker.API.DTOs;

/// <summary>Query parameters for listing tasks assigned to or reviewed by the current user.</summary>
public sealed record MyTaskQueryParameters(
    int? StatusId,
    string? Search,
    string? SortBy);

/// <summary>Request body for updating the status of a task the user owns.</summary>
public sealed record UpdateMyTaskStatusRequest(
    int TaskStatusId,
    string? Remarks);

/// <summary>Request body for adding a comment to a task the user is involved in.</summary>
public sealed record AddMyTaskCommentRequest(
    string CommentText);
