using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_Group3.Models;
using Project_Group3.ViewModel;

namespace Project_Group3.Controllers;

public class AdminAnalyticsController(CloneEbayDbContext dbContext) : Controller
{
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "superadmin",
        "monitor"
    };

    [HttpGet]
    public async Task<IActionResult> Analytics(
        string? periodType = null,
        DateTime? day = null,
        int? month = null,
        int? quarter = null,
        int? year = null,
        CancellationToken cancellationToken = default)
    {
        if (!HasAdminAccess())
        {
            return RedirectToAction("Login", "Account");
        }

        var today = DateTime.Today;
        var vm = new AnalyticsViewModel
        {
            PeriodType = NormalizePeriodType(periodType),
            Day = day?.Date ?? today,
            Month = month is >= 1 and <= 12 ? month.Value : today.Month,
            Quarter = quarter is >= 1 and <= 4 ? quarter.Value : ((today.Month - 1) / 3) + 1,
            Year = year is >= 2000 and <= 2100 ? year.Value : today.Year
        };

        (vm.RangeStart, vm.RangeEndExclusive) = ResolveRange(vm.PeriodType, vm.Day, vm.Month, vm.Quarter, vm.Year);

        vm.TotalOrders = await dbContext.OrderTables
            .Where(o => o.orderDate >= vm.RangeStart && o.orderDate < vm.RangeEndExclusive)
            .CountAsync(cancellationToken);

        vm.TotalRevenue = await dbContext.OrderTables
            .Where(o => o.orderDate >= vm.RangeStart && o.orderDate < vm.RangeEndExclusive)
            .SumAsync(o => o.totalPrice ?? 0m, cancellationToken);

        vm.NewUsers = await dbContext.Users
            .Where(u => u.createdAt >= vm.RangeStart && u.createdAt < vm.RangeEndExclusive)
            .CountAsync(cancellationToken);

        return View("~/Views/Admin/Analytics.cshtml", vm);
    }

    private static string NormalizePeriodType(string? periodType)
    {
        var normalized = periodType?.Trim().ToLowerInvariant();
        return normalized is "day" or "month" or "quarter" or "year" ? normalized : "month";
    }

    private static (DateTime Start, DateTime EndExclusive) ResolveRange(string periodType, DateTime day, int month, int quarter, int year)
        => periodType switch
        {
            "day" => (day.Date, day.Date.AddDays(1)),
            "month" => (new DateTime(year, month, 1), new DateTime(year, month, 1).AddMonths(1)),
            "quarter" => (new DateTime(year, ((quarter - 1) * 3) + 1, 1), new DateTime(year, ((quarter - 1) * 3) + 1, 1).AddMonths(3)),
            "year" => (new DateTime(year, 1, 1), new DateTime(year + 1, 1, 1)),
            _ => (new DateTime(year, month, 1), new DateTime(year, month, 1).AddMonths(1))
        };

    private bool HasAdminAccess()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var role = HttpContext.Session.GetString("Role");

        return userId is not null && AllowedRoles.Contains(role ?? string.Empty);
    }
}
