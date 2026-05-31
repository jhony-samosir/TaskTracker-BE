using System;
using System.Collections.Generic;

namespace TaskTracker.API.Models;

public partial class MstDocument
{
    public int DocumentId { get; set; }

    public DateTime CreatedDate { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime? DeletedDate { get; set; }

    public string? DeletedBy { get; set; }

    public string FileName { get; set; } = null!;

    public string MimeType { get; set; } = null!;

    public long? FileSize { get; set; }

    public string FileBase64 { get; set; } = null!;
}
