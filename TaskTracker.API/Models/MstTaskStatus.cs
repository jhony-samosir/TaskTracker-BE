using System;
using System.Collections.Generic;

namespace TaskTracker.API.Models;

public partial class MstTaskStatus
{
    public int TaskStatusId { get; set; }

    public DateTime CreatedDate { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime? DeletedDate { get; set; }

    public string? DeletedBy { get; set; }

    public string StatusName { get; set; } = null!;

    public string? StatusDescription { get; set; }
}
