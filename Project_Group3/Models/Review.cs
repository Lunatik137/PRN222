using System;
using System.Collections.Generic;

namespace PRN222_Group3.Models;

public partial class Review
{
    public int Id { get; set; }

    public int? ProductId { get; set; }

    public int? ReviewerId { get; set; }

    public int? Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Product? Product { get; set; }

    public virtual User? Reviewer { get; set; }
}
