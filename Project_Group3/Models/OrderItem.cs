using System;
using System.Collections.Generic;

namespace Project_Group3.Models;

public partial class OrderItem
{
    public int id { get; set; }

    public int? orderId { get; set; }

    public int? productId { get; set; }

    public int? quantity { get; set; }

    public decimal? unitPrice { get; set; }

    public virtual OrderTable? order { get; set; }

    public virtual Product? product { get; set; }
}
