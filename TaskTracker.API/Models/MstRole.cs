using System;
using System.Collections.Generic;

namespace TaskTracker.API.Models;

public partial class MstRole
{
    public int RoleId { get; set; }

    public DateTime CreatedDate { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime? DeletedDate { get; set; }

    public string? DeletedBy { get; set; }

    public string RoleName { get; set; } = null!;

    public string? RoleDescription { get; set; }
}
