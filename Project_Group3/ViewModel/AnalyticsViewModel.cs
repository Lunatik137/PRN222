namespace Project_Group3.ViewModel;

public sealed class AnalyticsViewModel
{
    public string PeriodType { get; set; } = "month";

    public DateTime Day { get; set; } = DateTime.Today;

    public int Month { get; set; } = DateTime.Today.Month;

    public int Quarter { get; set; } = ((DateTime.Today.Month - 1) / 3) + 1;

    public int Year { get; set; } = DateTime.Today.Year;

    public decimal TotalRevenue { get; set; }

    public int TotalOrders { get; set; }

    public int NewUsers { get; set; }

    public int TotalReturns { get; set; }

    public int TotalReviews { get; set; }

    public decimal AverageRating { get; set; }

    public DateTime RangeStart { get; set; }

    public DateTime RangeEndExclusive { get; set; }

    public List<TopSellerViewModel> TopSellers { get; set; } = new();

    public List<TopProductViewModel> TopProducts { get; set; } = new();

    public List<TopBuyerViewModel> TopBuyers { get; set; } = new();
}

public sealed class TopSellerViewModel
{
    public string Seller { get; set; } = string.Empty;

    public int Orders { get; set; }

    public int Items { get; set; }

    public decimal Revenue { get; set; }
}

public sealed class TopProductViewModel
{
    public string Product { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal Revenue { get; set; }
}

public sealed class TopBuyerViewModel
{
    public string Buyer { get; set; } = string.Empty;

    public int Orders { get; set; }

    public decimal TotalSpent { get; set; }
}
