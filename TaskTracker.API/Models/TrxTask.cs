using System;
using System.Collections.Generic;

namespace TaskTracker.API.Models;

public partial class TrxTask
{
    public int TaskId { get; set; }

    public DateTime CreatedDate { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime? DeletedDate { get; set; }

    public string? DeletedBy { get; set; }

    public string TaskTitle { get; set; } = null!;

    public string? TaskDescription { get; set; }

    public int AssigneeUserId { get; set; }

    public int ReviewerUserId { get; set; }

    public DateTime DeadlineDate { get; set; }

    public int TaskStatusId { get; set; }
}
