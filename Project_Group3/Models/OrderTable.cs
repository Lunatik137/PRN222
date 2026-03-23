using System;
using System.Collections.Generic;

namespace PRN222_Group3.Models;

public partial class OrderTable
{
    public int Id { get; set; }

    public int? BuyerId { get; set; }

    public int? AddressId { get; set; }

    public DateTime OrderDate { get; set; }

    public decimal TotalPrice { get; set; }

    public string? Status { get; set; }

    public virtual Address? Address { get; set; }

    public virtual User? Buyer { get; set; }

    public virtual ICollection<Dispute> Disputes { get; set; } = new List<Dispute>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<ReturnRequest> ReturnRequests { get; set; } = new List<ReturnRequest>();

    public virtual ICollection<ShippingInfo> ShippingInfos { get; set; } = new List<ShippingInfo>();
}
