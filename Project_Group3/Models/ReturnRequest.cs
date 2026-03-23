using System;
using System.Collections.Generic;

namespace PRN222_Group3.Models;

public partial class ReturnRequest
{
    public int Id { get; set; }

    public int? OrderId { get; set; }

    public int? UserId { get; set; }

    public string? Reason { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? Images { get; set; }

    public virtual OrderTable? Order { get; set; }

    public virtual User? User { get; set; }
}
