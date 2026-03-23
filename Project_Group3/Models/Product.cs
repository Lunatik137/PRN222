using System;
using System.Collections.Generic;

namespace PRN222_Group3.Models;

public partial class Product
{
    public int Id { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public string? Images { get; set; }

    public int? CategoryId { get; set; }

    public int? SellerId { get; set; }

    public bool? IsAuction { get; set; }

    public DateTime? AuctionEndTime { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<Bid> Bids { get; set; } = new List<Bid>();

    public virtual Category? Category { get; set; }

    public virtual ICollection<Coupon> Coupons { get; set; } = new List<Coupon>();

    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual User? Seller { get; set; }
}
