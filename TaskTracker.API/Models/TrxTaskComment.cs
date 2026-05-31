using System;
using System.Collections.Generic;

namespace TaskTracker.API.Models;

public partial class TrxTaskComment
{
    public int TaskCommentId { get; set; }

    public DateTime CreatedDate { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime? DeletedDate { get; set; }

    public string? DeletedBy { get; set; }

    public int TaskId { get; set; }

    public int ReviewerUserId { get; set; }

    public string CommentText { get; set; } = null!;
}
