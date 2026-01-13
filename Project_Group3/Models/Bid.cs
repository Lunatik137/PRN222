using System;
using System.Collections.Generic;

namespace Project_Group3.Models;

public partial class Bid
{
    public int id { get; set; }

    public int? productId { get; set; }

    public int? bidderId { get; set; }

    public decimal? amount { get; set; }

    public DateTime? bidTime { get; set; }

    public virtual User? bidder { get; set; }

    public virtual Product? product { get; set; }
}
