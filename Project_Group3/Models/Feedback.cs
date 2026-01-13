using System;
using System.Collections.Generic;

namespace Project_Group3.Models;

public partial class Feedback
{
    public int id { get; set; }

    public int? sellerId { get; set; }

    public decimal? averageRating { get; set; }

    public int? totalReviews { get; set; }

    public decimal? positiveRate { get; set; }

    public virtual User? seller { get; set; }
}
