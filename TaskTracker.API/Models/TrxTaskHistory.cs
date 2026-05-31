using System;
using System.Collections.Generic;

namespace TaskTracker.API.Models;

public partial class TrxTaskHistory
{
    public int TaskHistoryId { get; set; }

    public DateTime CreatedDate { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime? DeletedDate { get; set; }

    public string? DeletedBy { get; set; }

    public int TaskId { get; set; }

    public int? OldTaskStatusId { get; set; }

    public int NewTaskStatusId { get; set; }

    public string ActionType { get; set; } = null!;

    public string? Remarks { get; set; }
}
