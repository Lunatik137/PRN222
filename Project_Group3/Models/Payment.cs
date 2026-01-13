using System;
using System.Collections.Generic;

namespace Project_Group3.Models;

public partial class Payment
{
    public int id { get; set; }

    public int? orderId { get; set; }

    public int? userId { get; set; }

    public decimal? amount { get; set; }

    public string? method { get; set; }

    public string? status { get; set; }

    public DateTime? paidAt { get; set; }

    public virtual OrderTable? order { get; set; }

    public virtual User? user { get; set; }
}
