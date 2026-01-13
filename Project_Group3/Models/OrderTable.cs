using System;
using System.Collections.Generic;

namespace Project_Group3.Models;

public partial class OrderTable
{
    public int id { get; set; }

    public int? buyerId { get; set; }

    public int? addressId { get; set; }

    public DateTime? orderDate { get; set; }

    public decimal? totalPrice { get; set; }

    public string? status { get; set; }

    public virtual ICollection<Dispute> Disputes { get; set; } = new List<Dispute>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<ReturnRequest> ReturnRequests { get; set; } = new List<ReturnRequest>();

    public virtual ICollection<ShippingInfo> ShippingInfos { get; set; } = new List<ShippingInfo>();

    public virtual Address? address { get; set; }

    public virtual User? buyer { get; set; }
}
