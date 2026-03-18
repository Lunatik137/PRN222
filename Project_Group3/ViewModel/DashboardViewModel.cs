namespace Project_Group3.ViewModel;

public sealed class DashboardViewModel
{
    public int TotalUsers { get; set; }

    public int TotalProducts { get; set; }

    public int TotalOrders { get; set; }

    // Future extension points for upcoming dashboard phases.
    public decimal? TotalRevenue { get; set; }

    public int? NewUsersToday { get; set; }

    public int? OrdersToday { get; set; }

    public List<DashboardWidgetViewModel> Widgets { get; set; } = new();

    public List<DashboardActivityViewModel> RecentActivities { get; set; } = new();

    public List<DashboardAlertViewModel> Alerts { get; set; } = new();
}

public sealed class DashboardWidgetViewModel
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
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
