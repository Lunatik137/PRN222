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

    public DateTime RangeStart { get; set; }

    public DateTime RangeEndExclusive { get; set; }

    // Extensible chart data placeholders for future Chart.js integration.
    public List<string> ChartLabels { get; set; } = new();

    public List<decimal> RevenueSeries { get; set; } = new();

    public List<int> OrdersSeries { get; set; } = new();

    public List<int> NewUsersSeries { get; set; } = new();
}
