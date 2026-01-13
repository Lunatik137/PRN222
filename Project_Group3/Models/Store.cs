using System;
using System.Collections.Generic;

namespace Project_Group3.Models;

public partial class Store
{
    public int id { get; set; }

    public int? sellerId { get; set; }

    public string? storeName { get; set; }

    public string? description { get; set; }

    public string? bannerImageURL { get; set; }

    public byte storeLevel { get; set; }

    public virtual ICollection<StoreUpgradeRequest> StoreUpgradeRequests { get; set; } = new List<StoreUpgradeRequest>();

    public virtual User? seller { get; set; }
}
