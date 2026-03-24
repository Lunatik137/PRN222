using System;
using System.Collections.Generic;

namespace Project_Group3.Models;

public partial class Product
{
    public int id { get; set; }

    public string? title { get; set; }

    public string? description { get; set; }

    public decimal? price { get; set; }

    public string? images { get; set; }

    public int? categoryId { get; set; }

    public int? sellerId { get; set; }

    public bool? isAuction { get; set; }

    public DateTime? auctionEndTime { get; set; }

    public string? status { get; set; }

    public string? reason { get; set; }

    public int? reportnumber { get; set; }

    public virtual ICollection<Bid> Bids { get; set; } = new List<Bid>();

    public virtual ICollection<Coupon> Coupons { get; set; } = new List<Coupon>();

    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual Category? category { get; set; }

    public virtual User? seller { get; set; }
}
