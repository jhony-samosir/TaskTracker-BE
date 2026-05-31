using TaskTracker.API.DTOs;

namespace TaskTracker.API.Interfaces;

/// <summary>Service for task operations scoped to the currently authenticated user (assignee or reviewer).</summary>
public interface IMyTaskService
{
    /// <summary>Retrieves tasks assigned to or reviewed by the current user.</summary>
    Task<IEnumerable<TaskResponse>> GetMyTasksAsync(
        int currentUserId,
        int? statusId,
        string? search,
        string? sortBy,
        CancellationToken cancellationToken);

    /// <summary>Retrieves a single task by id, only if the current user is the assignee or reviewer.</summary>
    Task<TaskResponse> GetMyTaskByIdAsync(
        int currentUserId,
        int taskId,
        CancellationToken cancellationToken);

    /// <summary>Updates the status of a task owned by the current user, enforcing transition rules.</summary>
    Task<TaskResponse> UpdateMyTaskStatusAsync(
        int currentUserId,
        int taskId,
        UpdateMyTaskStatusRequest request,
        CancellationToken cancellationToken);

    /// <summary>Adds a comment to a task the current user is involved in.</summary>
    Task<TaskCommentResponse> AddMyTaskCommentAsync(
        int currentUserId,
        int taskId,
        AddMyTaskCommentRequest request,
        CancellationToken cancellationToken);
}
