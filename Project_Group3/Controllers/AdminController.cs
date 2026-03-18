using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_Group3.Models;
using Project_Group3.ViewModel;

namespace Project_Group3.Controllers;

public class AdminController(CloneEbayDbContext context) : Controller
{
    private readonly CloneEbayDbContext _context = context;

    [HttpGet]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        if (HttpContext.Session.GetInt32("UserId") is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var totalUsers = await _context.Users.CountAsync(cancellationToken);
        var totalProducts = await _context.Products.CountAsync(cancellationToken);
        var totalOrders = await _context.OrderTables.CountAsync(cancellationToken);

        var viewModel = new DashboardViewModel
        {
            TotalUsers = totalUsers,
            TotalProducts = totalProducts,
            TotalOrders = totalOrders
        };

        return View(viewModel);
    }

    [HttpGet]
    public IActionResult UserManagement() => AdminSection("User Management");

    [HttpGet]
    public IActionResult ProductModeration() => AdminSection("Product Moderation");

    [HttpGet]
    public IActionResult OrderManagement() => AdminSection("Order Management");

    [HttpGet]
    public IActionResult ReviewsFeedback() => AdminSection("Reviews & Feedback");

    [HttpGet]
    public IActionResult ComplaintsDisputes() => AdminSection("Complaints / Disputes");

    [HttpGet]
    public async Task<IActionResult> Analytics(
        string periodType = "month",
        DateTime? day = null,
        int? month = null,
        int? quarter = null,
        int? year = null,
        CancellationToken cancellationToken = default)
    {
        if (HttpContext.Session.GetInt32("UserId") is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var normalizedPeriodType = NormalizePeriodType(periodType);
        var today = DateTime.Today;

        var selectedDay = day?.Date ?? today;
        var selectedYear = year ?? today.Year;
        var selectedMonth = month is >= 1 and <= 12 ? month.Value : today.Month;
        var selectedQuarter = quarter is >= 1 and <= 4 ? quarter.Value : ((today.Month - 1) / 3) + 1;

        var (start, endExclusive) = ResolveRange(normalizedPeriodType, selectedDay, selectedMonth, selectedQuarter, selectedYear);

        var ordersInRange = _context.OrderTables
            .Where(x => x.orderDate != null && x.orderDate >= start && x.orderDate < endExclusive);

        var totalRevenue = await ordersInRange.SumAsync(x => (decimal?)x.totalPrice, cancellationToken) ?? 0m;
        var totalOrders = await ordersInRange.CountAsync(cancellationToken);
        var newUsers = await _context.Users
            .Where(x => x.createdAt >= start && x.createdAt < endExclusive)
            .CountAsync(cancellationToken);

        var model = new AnalyticsViewModel
        {
            PeriodType = normalizedPeriodType,
            Day = selectedDay,
            Month = selectedMonth,
            Quarter = selectedQuarter,
            Year = selectedYear,
            TotalRevenue = totalRevenue,
            TotalOrders = totalOrders,
            NewUsers = newUsers,
            RangeStart = start,
            RangeEndExclusive = endExclusive
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult SystemSettings() => AdminSection("System Settings");

    private IActionResult AdminSection(string sectionTitle)
    {
        if (HttpContext.Session.GetInt32("UserId") is null)
        {
            return RedirectToAction("Login", "Account");
        }

        ViewData["SectionTitle"] = sectionTitle;
        return View("Section");
    }

    private static string NormalizePeriodType(string periodType)
    {
        if (string.Equals(periodType, "day", StringComparison.OrdinalIgnoreCase))
        {
            return "day";
        }

        if (string.Equals(periodType, "quarter", StringComparison.OrdinalIgnoreCase))
        {
            return "quarter";
        }

        if (string.Equals(periodType, "year", StringComparison.OrdinalIgnoreCase))
        {
            return "year";
        }

        return "month";
    }

    private static (DateTime Start, DateTime EndExclusive) ResolveRange(
        string periodType,
        DateTime selectedDay,
        int selectedMonth,
        int selectedQuarter,
        int selectedYear)
    {
        if (periodType == "day")
        {
            var start = selectedDay.Date;
            return (start, start.AddDays(1));
        }

        if (periodType == "quarter")
        {
            var quarterStartMonth = ((selectedQuarter - 1) * 3) + 1;
            var start = new DateTime(selectedYear, quarterStartMonth, 1);
            return (start, start.AddMonths(3));
        }

        if (periodType == "year")
        {
            var start = new DateTime(selectedYear, 1, 1);
            return (start, start.AddYears(1));
        }

        var monthStart = new DateTime(selectedYear, selectedMonth, 1);
        return (monthStart, monthStart.AddMonths(1));
    }
}
