using System;
using System.Collections.Generic;

namespace PRN222_Group3.Models;

public partial class Inventory
{
    public int Id { get; set; }

    public int? ProductId { get; set; }

    public int? Quantity { get; set; }

    public DateTime? LastUpdated { get; set; }

    public virtual Product? Product { get; set; }
}
