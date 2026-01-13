using System;
using System.Collections.Generic;

namespace Project_Group3.Models;

public partial class Coupon
{
    public int id { get; set; }

    public string? code { get; set; }

    public decimal? discountPercent { get; set; }

    public DateTime? startDate { get; set; }

    public DateTime? endDate { get; set; }

    public int? maxUsage { get; set; }

    public int? productId { get; set; }

    public virtual Product? product { get; set; }
}
