using System;
using System.Collections.Generic;

namespace PRN222_Group3.Models;

public partial class StoreUpgradeRequest
{
    public int Id { get; set; }

    public int StoreId { get; set; }

    public byte RequestedLevel { get; set; }

    public byte Status { get; set; } 

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DecidedAt { get; set; }

    public int? DecidedByAdminId { get; set; }

    public virtual User? DecidedByAdmin { get; set; }

    public virtual Store Store { get; set; } = null!;
}
