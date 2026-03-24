using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_Group3.Models;
using Project_Group3.ViewModel;

namespace Project_Group3.Controllers;

public class AdminDashboardController(CloneEbayDbContext dbContext) : Controller
{
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "superadmin",
        "monitor"
    };

    [HttpGet]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        if (!HasAdminAccess())
        {
            return RedirectToAction("Login", "Account");
        }

        var nowUtc = DateTime.UtcNow;
        var todayUtc = nowUtc.Date;
        var yesterdayUtc = todayUtc.AddDays(-1);
        var tomorrowUtc = todayUtc.AddDays(1);

        var totalUsers = await dbContext.Users.CountAsync(cancellationToken);
        var totalProducts = await dbContext.Products.CountAsync(cancellationToken);
        var totalOrders = await dbContext.OrderTables.CountAsync(cancellationToken);
        var totalRevenue = await dbContext.OrderTables.SumAsync(o => o.totalPrice ?? 0m, cancellationToken);
        var newUsersToday = await dbContext.Users.CountAsync(u => u.createdAt >= todayUtc && u.createdAt < tomorrowUtc, cancellationToken);
        var ordersToday = await dbContext.OrderTables.CountAsync(o => o.orderDate >= todayUtc && o.orderDate < tomorrowUtc, cancellationToken);
        var newUsersYesterday = await dbContext.Users.CountAsync(u => u.createdAt >= yesterdayUtc && u.createdAt < todayUtc, cancellationToken);
        var ordersYesterday = await dbContext.OrderTables.CountAsync(o => o.orderDate >= yesterdayUtc && o.orderDate < todayUtc, cancellationToken);

        var ordersTrendStart = todayUtc.AddDays(-6);
        var ordersTrendRaw = await dbContext.OrderTables
            .Where(o => o.orderDate >= ordersTrendStart && o.orderDate < tomorrowUtc)
            .Select(o => o.orderDate)
            .ToListAsync(cancellationToken);

        var usersTrendStart = new DateTime(todayUtc.Year, todayUtc.Month, 1).AddMonths(-5);
        var usersTrendRaw = await dbContext.Users
            .Where(u => u.createdAt >= usersTrendStart && u.createdAt < tomorrowUtc)
            .Select(u => u.createdAt)
            .ToListAsync(cancellationToken);

        var categoryProducts = await dbContext.Products
            .Include(p => p.category)
            .Select(p => new
            {
                CategoryName = p.category != null && !string.IsNullOrWhiteSpace(p.category.name)
                    ? p.category.name!
                    : "Uncategorized"
            })
            .ToListAsync(cancellationToken);

        var latestUsers = await dbContext.Users
            .OrderByDescending(u => u.createdAt)
            .Select(u => new { u.username, u.createdAt })
            .Take(5)
            .ToListAsync(cancellationToken);

        var latestOrders = await dbContext.OrderTables
            .OrderByDescending(o => o.orderDate)
            .Select(o => new { o.id, o.orderDate, o.status })
            .Take(5)
            .ToListAsync(cancellationToken);

        var latestReturns = await dbContext.ReturnRequests
            .OrderByDescending(r => r.createdAt)
            .Select(r => new { r.id, r.createdAt, r.status })
            .Take(5)
            .ToListAsync(cancellationToken);

        var latestReviews = await dbContext.Reviews
            .OrderByDescending(r => r.createdAt)
            .Select(r => new { r.id, r.createdAt, r.rating })
            .Take(5)
            .ToListAsync(cancellationToken);

        var reportedProducts = await dbContext.Products.CountAsync(p => (p.status ?? string.Empty).ToLower() == "reported", cancellationToken);
        var pendingReturns = await dbContext.ReturnRequests.CountAsync(r => (r.status ?? string.Empty).ToLower() == "pending", cancellationToken);
        var openDisputes = await dbContext.Disputes.CountAsync(d =>
            (d.status ?? string.Empty).ToLower() == "open"
            || (d.status ?? string.Empty).ToLower() == "pending", cancellationToken);
        var highRiskUsers = await dbContext.Users.CountAsync(u =>
            (u.RiskScore ?? 0) >= 70
            || (u.RiskLevel ?? string.Empty).ToLower() == "high"
            || (u.RiskLevel ?? string.Empty).ToLower() == "critical", cancellationToken);

        var ordersTrendLabels = new List<string>();
        var ordersTrendSeries = new List<int>();
        for (var i = 0; i < 7; i++)
        {
            var date = ordersTrendStart.AddDays(i);
            ordersTrendLabels.Add(date.ToString("dd/MM"));
            ordersTrendSeries.Add(ordersTrendRaw.Count(d => d?.Date == date));
        }

        var newUsersLabels = new List<string>();
        var newUsersSeries = new List<int>();
        for (var i = 0; i < 6; i++)
        {
            var monthStart = usersTrendStart.AddMonths(i);
            var monthEnd = monthStart.AddMonths(1);
            newUsersLabels.Add(monthStart.ToString("MMM yyyy"));
            newUsersSeries.Add(usersTrendRaw.Count(d => d >= monthStart && d < monthEnd));
        }

        var categoryRows = categoryProducts
            .GroupBy(x => x.CategoryName)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(4)
            .ToList();

        var categoryTotal = categoryRows.Sum(x => x.Count);
        var categoryDistribution = categoryRows
            .Select(x => new DashboardCategoryDistributionViewModel
            {
                Name = x.Name,
                Count = x.Count,
                Percentage = categoryTotal == 0 ? 0m : decimal.Round((x.Count * 100m) / categoryTotal, 1)
            })
            .ToList();

        var activityItems = new List<(DateTime Time, string Title)>();
        activityItems.AddRange(latestUsers.Select(x =>
            (x.createdAt, $"New user registered: {(!string.IsNullOrWhiteSpace(x.username) ? x.username : "Unknown user")}")));

        activityItems.AddRange(latestOrders
            .Where(x => x.orderDate.HasValue)
            .Select(x => (x.orderDate!.Value, $"Order #{x.id} updated ({(string.IsNullOrWhiteSpace(x.status) ? "unknown" : x.status)})")));

        activityItems.AddRange(latestReturns
            .Where(x => x.createdAt.HasValue)
            .Select(x => (x.createdAt!.Value, $"Return request #{x.id} ({(string.IsNullOrWhiteSpace(x.status) ? "unknown" : x.status)})")));

        activityItems.AddRange(latestReviews
            .Where(x => x.createdAt.HasValue)
            .Select(x => (x.createdAt!.Value, $"Review #{x.id} submitted (rating {(x.rating ?? 0)}/5)")));

        var recentActivities = activityItems
            .OrderByDescending(x => x.Time)
            .Take(6)
            .Select(x => new DashboardActivityViewModel
            {
                Title = x.Title,
                TimeLabel = ToRelativeTime(x.Time, nowUtc)
            })
            .ToList();

        var alerts = new List<DashboardAlertViewModel>
        {
            new()
            {
                Message = $"{reportedProducts} products currently flagged as reported.",
                Level = reportedProducts >= 15 ? "High" : reportedProducts >= 5 ? "Warning" : "Info"
            },
            new()
            {
                Message = $"{pendingReturns} return requests are pending review.",
                Level = pendingReturns >= 20 ? "High" : pendingReturns >= 8 ? "Warning" : "Info"
            },
            new()
            {
                Message = $"{openDisputes} disputes are currently open/pending.",
                Level = openDisputes >= 10 ? "High" : openDisputes >= 4 ? "Warning" : "Info"
            },
            new()
            {
                Message = $"{highRiskUsers} users are marked high risk.",
                Level = highRiskUsers >= 10 ? "High" : highRiskUsers >= 3 ? "Warning" : "Info"
            }
        };

        var vm = new DashboardViewModel
        {
            TotalUsers = totalUsers,
            TotalProducts = totalProducts,
            TotalOrders = totalOrders,
            TotalRevenue = totalRevenue,
            NewUsersToday = newUsersToday,
            OrdersToday = ordersToday,
            NewUsersYesterday = newUsersYesterday,
            OrdersYesterday = ordersYesterday,
            NewUsersDeltaPercent = CalculateDeltaPercent(newUsersToday, newUsersYesterday),
            OrdersDeltaPercent = CalculateDeltaPercent(ordersToday, ordersYesterday),
            OrdersTrendLabels = ordersTrendLabels,
            OrdersTrendSeries = ordersTrendSeries,
            NewUsersLabels = newUsersLabels,
            NewUsersSeries = newUsersSeries,
            CategoryDistribution = categoryDistribution,
            RecentActivities = recentActivities,
            Alerts = alerts
        };

        return View("~/Views/Admin/Dashboard.cshtml", vm);
    }

    private bool HasAdminAccess()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var role = HttpContext.Session.GetString("Role");
        var isAdminTwoFactorVerified = HttpContext.Session.GetString("IsAdmin2FAVerified");

        return userId is not null
            && AllowedRoles.Contains(role ?? string.Empty)
            && string.Equals(isAdminTwoFactorVerified, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static string ToRelativeTime(DateTime timeUtc, DateTime nowUtc)
    {
        var diff = nowUtc - timeUtc;
        if (diff < TimeSpan.FromMinutes(1))
        {
            return "just now";
        }

        if (diff < TimeSpan.FromHours(1))
        {
            return $"{Math.Max(1, (int)diff.TotalMinutes)} mins ago";
        }

        if (diff < TimeSpan.FromDays(1))
        {
            return $"{Math.Max(1, (int)diff.TotalHours)} hours ago";
        }

        return $"{Math.Max(1, (int)diff.TotalDays)} days ago";
    }

    private static decimal CalculateDeltaPercent(int today, int yesterday)
    {
        if (yesterday <= 0)
        {
            return today <= 0 ? 0m : 100m;
        }

        return decimal.Round(((today - yesterday) * 100m) / yesterday, 1);
    }
}
