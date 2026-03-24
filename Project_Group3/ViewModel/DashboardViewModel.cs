namespace Project_Group3.ViewModel;

public sealed class DashboardViewModel
{
    public int TotalUsers { get; set; }

    public int TotalProducts { get; set; }

    public int TotalOrders { get; set; }

    public decimal TotalRevenue { get; set; }

    public int NewUsersToday { get; set; }

    public int OrdersToday { get; set; }

    public List<string> OrdersTrendLabels { get; set; } = new();

    public List<int> OrdersTrendSeries { get; set; } = new();

    public List<string> NewUsersLabels { get; set; } = new();

    public List<int> NewUsersSeries { get; set; } = new();

    public List<DashboardActivityViewModel> RecentActivities { get; set; } = new();

    public List<DashboardAlertViewModel> Alerts { get; set; } = new();

    public List<DashboardCategoryDistributionViewModel> CategoryDistribution { get; set; } = new();
}

public sealed class DashboardActivityViewModel
{
    public string Title { get; set; } = string.Empty;

    public string TimeLabel { get; set; } = string.Empty;
}

public sealed class DashboardAlertViewModel
{
    public string Message { get; set; } = string.Empty;

    public string Level { get; set; } = string.Empty;
}

public sealed class DashboardCategoryDistributionViewModel
{
    public string Name { get; set; } = string.Empty;

    public int Count { get; set; }

    public decimal Percentage { get; set; }
}
