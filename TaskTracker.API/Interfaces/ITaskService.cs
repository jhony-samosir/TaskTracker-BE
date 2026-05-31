using TaskTracker.API.DTOs;

namespace TaskTracker.API.Interfaces;

public interface ITaskService
{
    Task<IEnumerable<TaskResponse>> GetTasksAsync(
        int? assigneeId,
        int? statusId,
        string? search,
        int currentUserId,
        string currentUserRole,
        CancellationToken cancellationToken);

    Task<TaskResponse> GetTaskByIdAsync(
        int taskId,
        int currentUserId,
        string currentUserRole,
        CancellationToken cancellationToken);

    Task<TaskResponse> CreateTaskAsync(
        int currentUserId,
        CreateTaskRequest request,
        CancellationToken cancellationToken);

    Task<TaskResponse> UpdateTaskAsync(
        int currentUserId,
        int taskId,
        UpdateTaskRequest request,
        CancellationToken cancellationToken);

    Task<bool> DeleteTaskAsync(
        int currentUserId,
        int taskId,
        CancellationToken cancellationToken);

    Task<TaskResponse> UpdateTaskStatusAsync(
        int currentUserId,
        int taskId,
        UpdateTaskStatusRequest request,
        CancellationToken cancellationToken);

    Task<TaskCommentResponse> AddCommentAsync(
        int currentUserId,
        int taskId,
        AddCommentRequest request,
        CancellationToken cancellationToken);

    Task<TaskDocumentResponse> AttachDocumentAsync(
        int currentUserId,
        int taskId,
        AttachTaskDocumentRequest request,
        CancellationToken cancellationToken);
}
