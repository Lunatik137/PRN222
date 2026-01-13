using System;
using System.Collections.Generic;

namespace Project_Group3.Models;

public partial class StoreUpgradeRequest
{
    public int id { get; set; }

    public int storeId { get; set; }

    public byte requestedLevel { get; set; }

    public byte status { get; set; }

    public string? note { get; set; }

    public DateTime createdAt { get; set; }

    public DateTime? decidedAt { get; set; }

    public int? decidedByAdminId { get; set; }

    public virtual User? decidedByAdmin { get; set; }

    public virtual Store store { get; set; } = null!;
}
