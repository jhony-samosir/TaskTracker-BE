using System;
using System.Collections.Generic;

namespace TaskTracker.API.Models;

public partial class TrxTaskDocument
{
    public int TaskDocumentId { get; set; }

    public DateTime CreatedDate { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime? DeletedDate { get; set; }

    public string? DeletedBy { get; set; }

    public int TaskId { get; set; }

    public int DocumentId { get; set; }

    public string DocumentType { get; set; } = null!;

    public int DocumentVersion { get; set; }
}
