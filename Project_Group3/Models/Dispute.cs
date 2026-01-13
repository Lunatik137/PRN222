using System;
using System.Collections.Generic;

namespace Project_Group3.Models;

public partial class Dispute
{
    public int id { get; set; }

    public int? orderId { get; set; }

    public int? raisedBy { get; set; }

    public string? description { get; set; }

    public string? status { get; set; }

    public string? resolution { get; set; }

    public virtual OrderTable? order { get; set; }

    public virtual User? raisedByNavigation { get; set; }
}
