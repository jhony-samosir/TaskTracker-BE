using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.API.DTOs;
using TaskTracker.API.Interfaces;

namespace TaskTracker.API.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize]
public sealed class TaskController(ITaskService taskService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetTasks(
        [FromQuery] int? assigneeId,
        [FromQuery] int? statusId,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role);

        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized();

        var role = roleClaim?.Value ?? "User";

        var tasks = await taskService.GetTasksAsync(
            assigneeId,
            statusId,
            search,
            userId,
            role,
            cancellationToken);

        return Ok(ApiResponse<IEnumerable<TaskResponse>>.Success(tasks, "Tasks retrieved successfully."));
    }

    [HttpGet("{taskId:int}")]
    public async Task<IActionResult> GetTaskById(
        int taskId,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role);

        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized();

        var role = roleClaim?.Value ?? "User";

        var task = await taskService.GetTaskByIdAsync(
            taskId,
            userId,
            role,
            cancellationToken);

        return Ok(ApiResponse<TaskResponse>.Success(task, "Task retrieved successfully."));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateTask(
        [FromBody] CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized();

        var task = await taskService.CreateTaskAsync(
            userId,
            request,
            cancellationToken);

        return Ok(ApiResponse<TaskResponse>.Success(task, "Task created successfully."));
    }

    [HttpPut("{taskId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateTask(
        int taskId,
        [FromBody] UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized();

        var task = await taskService.UpdateTaskAsync(
            userId,
            taskId,
            request,
            cancellationToken);

        return Ok(ApiResponse<TaskResponse>.Success(task, "Task updated successfully."));
    }

    [HttpDelete("{taskId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteTask(
        int taskId,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized();

        var result = await taskService.DeleteTaskAsync(
            userId,
            taskId,
            cancellationToken);

        if (result)
            return Ok(ApiResponse<bool>.Success(true, "Task deleted successfully."));
        else
            return BadRequest(ApiResponse<bool>.Failure("Failed to delete task."));
    }

    [HttpPut("{taskId:int}/status")]
    public async Task<IActionResult> UpdateTaskStatus(
        int taskId,
        [FromBody] UpdateTaskStatusRequest request,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized();

        var task = await taskService.UpdateTaskStatusAsync(
            userId,
            taskId,
            request,
            cancellationToken);

        return Ok(ApiResponse<TaskResponse>.Success(task, "Task status updated successfully."));
    }

    [HttpPost("{taskId:int}/comments")]
    public async Task<IActionResult> AddComment(
        int taskId,
        [FromBody] AddCommentRequest request,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized();

        var comment = await taskService.AddCommentAsync(
            userId,
            taskId,
            request,
            cancellationToken);

        return Ok(ApiResponse<TaskCommentResponse>.Success(comment, "Comment added successfully."));
    }

    [HttpPost("{taskId:int}/documents")]
    public async Task<IActionResult> AttachDocument(
        int taskId,
        [FromBody] AttachTaskDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized();

        var document = await taskService.AttachDocumentAsync(
            userId,
            taskId,
            request,
            cancellationToken);

        return Ok(ApiResponse<TaskDocumentResponse>.Success(document, "Document attached successfully."));
    }
}
