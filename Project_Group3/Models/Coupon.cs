using System;
using System.Collections.Generic;

namespace PRN222_Group3.Models;

public partial class Coupon
{
    public int Id { get; set; }

    public string? Code { get; set; }

    public decimal? DiscountPercent { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int? MaxUsage { get; set; }

    public int? ProductId { get; set; }

    public virtual Product? Product { get; set; }
}
