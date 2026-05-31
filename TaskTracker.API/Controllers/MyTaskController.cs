using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskTracker.API.DTOs;
using TaskTracker.API.Interfaces;

namespace TaskTracker.API.Controllers;

[ApiController]
[Route("api/my-tasks")]
[Authorize]
public sealed class MyTaskController(IMyTaskService myTaskService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMyTasks(
        [FromQuery] MyTaskQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim, out var currentUserId))
            return Unauthorized();

        var tasks = await myTaskService.GetMyTasksAsync(
            currentUserId,
            parameters.StatusId,
            parameters.Search,
            parameters.SortBy,
            cancellationToken);

        return Ok(ApiResponse<IEnumerable<TaskResponse>>.Success(tasks, "Tasks retrieved successfully."));
    }

    [HttpGet("{taskId:int}")]
    public async Task<IActionResult> GetMyTaskById(
        int taskId,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim, out var currentUserId))
            return Unauthorized();

        var task = await myTaskService.GetMyTaskByIdAsync(
            currentUserId,
            taskId,
            cancellationToken);

        return Ok(ApiResponse<TaskResponse>.Success(task, "Task retrieved successfully."));
    }

    [HttpPut("{taskId:int}/status")]
    public async Task<IActionResult> UpdateMyTaskStatus(
        int taskId,
        [FromBody] UpdateMyTaskStatusRequest request,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim, out var currentUserId))
            return Unauthorized();

        var task = await myTaskService.UpdateMyTaskStatusAsync(
            currentUserId,
            taskId,
            request,
            cancellationToken);

        return Ok(ApiResponse<TaskResponse>.Success(task, "Task status updated successfully."));
    }

    [HttpPost("{taskId:int}/comments")]
    public async Task<IActionResult> AddMyTaskComment(
        int taskId,
        [FromBody] AddMyTaskCommentRequest request,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim, out var currentUserId))
            return Unauthorized();

        var comment = await myTaskService.AddMyTaskCommentAsync(
            currentUserId,
            taskId,
            request,
            cancellationToken);

        return Ok(ApiResponse<TaskCommentResponse>.Success(comment, "Comment added successfully."));
    }
}
