using System;
using System.Collections.Generic;

namespace Project_Group3.Models;

public partial class Category
{
    public int id { get; set; }

    public string? name { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
