using System;
using System.Collections.Generic;

namespace Project_Group3.Models;

public partial class Review
{
    public int id { get; set; }

    public int? productId { get; set; }

    public int? reviewerId { get; set; }

    public int? rating { get; set; }

    public string? comment { get; set; }

    public DateTime? createdAt { get; set; }

    public virtual Product? product { get; set; }

    public virtual User? reviewer { get; set; }
}
