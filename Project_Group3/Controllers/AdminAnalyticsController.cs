using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_Group3.Models;
using Project_Group3.ViewModel;
using System.Globalization;
using System.Text;

namespace Project_Group3.Controllers;

public class AdminAnalyticsController(CloneEbayDbContext dbContext) : Controller
{
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "superadmin",
        "monitor"
    };

    private static readonly HashSet<string> AllowedPeriodTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "day",
        "month",
        "quarter",
        "year"
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

        var vm = CreateAnalyticsRequestModel(periodType, day, month, quarter, year);
        await PopulateAnalyticsAsync(vm, cancellationToken);

        return View("~/Views/Admin/Analytics.cshtml", vm);
    }

    [HttpGet]
    public async Task<IActionResult> ExportReport(
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

        var vm = CreateAnalyticsRequestModel(periodType, day, month, quarter, year);
        await PopulateAnalyticsAsync(vm, cancellationToken);

        var fileName = $"analytics-report-{vm.PeriodType}-{DateTime.UtcNow:yyyyMMddHHmmss}.xls";
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(BuildExcel(vm))).ToArray();
        return File(bytes, "application/vnd.ms-excel", fileName);
    }

    private static AnalyticsViewModel CreateAnalyticsRequestModel(string? periodType, DateTime? day, int? month, int? quarter, int? year)
    {
        var today = DateTime.Today;
        var model = new AnalyticsViewModel
        {
            PeriodType = NormalizePeriodType(periodType),
            Day = day?.Date ?? today,
            Month = month is >= 1 and <= 12 ? month.Value : today.Month,
            Quarter = quarter is >= 1 and <= 4 ? quarter.Value : ((today.Month - 1) / 3) + 1,
            Year = year is >= 2000 and <= 2100 ? year.Value : today.Year
        };

        (model.RangeStart, model.RangeEndExclusive) = ResolveRange(model.PeriodType, model.Day, model.Month, model.Quarter, model.Year);
        return model;
    }

    private static string NormalizePeriodType(string? periodType)
    {
        var normalized = periodType?.Trim().ToLowerInvariant() ?? string.Empty;
        return AllowedPeriodTypes.Contains(normalized) ? normalized : "month";
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

    private async Task PopulateAnalyticsAsync(AnalyticsViewModel vm, CancellationToken cancellationToken)
    {
        var ordersQuery = dbContext.OrderTables
            .AsNoTracking()
            .Where(o => o.orderDate.HasValue && o.orderDate >= vm.RangeStart && o.orderDate < vm.RangeEndExclusive);

        vm.TotalOrders = await ordersQuery.CountAsync(cancellationToken);

        vm.TotalRevenue = await ordersQuery.SumAsync(o => o.totalPrice ?? 0m, cancellationToken);

        vm.NewUsers = await dbContext.Users
            .AsNoTracking()
            .CountAsync(u => u.createdAt >= vm.RangeStart && u.createdAt < vm.RangeEndExclusive, cancellationToken);

        vm.TotalReturns = await dbContext.ReturnRequests
            .AsNoTracking()
            .CountAsync(r => r.createdAt.HasValue && r.createdAt >= vm.RangeStart && r.createdAt < vm.RangeEndExclusive, cancellationToken);

        var reviewRatings = await dbContext.Reviews
            .AsNoTracking()
            .Where(r => r.createdAt.HasValue && r.createdAt >= vm.RangeStart && r.createdAt < vm.RangeEndExclusive)
            .Select(r => r.rating)
            .ToListAsync(cancellationToken);

        vm.TotalReviews = reviewRatings.Count;
        var ratedReviews = reviewRatings.Where(x => x.HasValue).Select(x => x!.Value).ToList();
        vm.AverageRating = ratedReviews.Count == 0 ? 0m : decimal.Round((decimal)ratedReviews.Average(), 2);

        var orderItemsRaw = await dbContext.OrderItems
            .AsNoTracking()
            .Where(oi =>
                oi.order != null
                && oi.order.orderDate.HasValue
                && oi.order.orderDate >= vm.RangeStart
                && oi.order.orderDate < vm.RangeEndExclusive)
            .Select(oi => new
            {
                OrderId = oi.orderId ?? 0,
                BuyerId = oi.order != null ? (oi.order.buyerId ?? 0) : 0,
                BuyerName = oi.order != null && oi.order.buyer != null ? (oi.order.buyer.username ?? string.Empty) : string.Empty,
                SellerId = oi.product != null ? (oi.product.sellerId ?? 0) : 0,
                SellerName = oi.product != null && oi.product.seller != null ? (oi.product.seller.username ?? string.Empty) : string.Empty,
                ProductId = oi.productId ?? 0,
                ProductTitle = oi.product != null ? (oi.product.title ?? string.Empty) : string.Empty,
                Quantity = oi.quantity ?? 0,
                Revenue = (oi.quantity ?? 0) * (oi.unitPrice ?? 0m)
            })
            .ToListAsync(cancellationToken);

        vm.TopSellers = orderItemsRaw
            .Where(x => x.SellerId > 0)
            .GroupBy(x => new { x.SellerId, x.SellerName })
            .Select(g => new TopSellerViewModel
            {
                Seller = string.IsNullOrWhiteSpace(g.Key.SellerName) ? $"seller_{g.Key.SellerId}" : g.Key.SellerName,
                Orders = g.Select(x => x.OrderId).Distinct().Count(),
                Items = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.Revenue)
            })
            .OrderByDescending(x => x.Revenue)
            .ThenByDescending(x => x.Orders)
            .Take(5)
            .ToList();

        vm.TopProducts = orderItemsRaw
            .Where(x => x.ProductId > 0)
            .GroupBy(x => new { x.ProductId, x.ProductTitle })
            .Select(g => new TopProductViewModel
            {
                Product = string.IsNullOrWhiteSpace(g.Key.ProductTitle) ? $"Product #{g.Key.ProductId}" : g.Key.ProductTitle,
                Quantity = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.Revenue)
            })
            .OrderByDescending(x => x.Quantity)
            .ThenByDescending(x => x.Revenue)
            .Take(5)
            .ToList();

        vm.TopBuyers = orderItemsRaw
            .Where(x => x.BuyerId > 0)
            .GroupBy(x => new { x.BuyerId, x.BuyerName })
            .Select(g => new TopBuyerViewModel
            {
                Buyer = string.IsNullOrWhiteSpace(g.Key.BuyerName) ? $"buyer_{g.Key.BuyerId}" : g.Key.BuyerName,
                Orders = g.Select(x => x.OrderId).Distinct().Count(),
                TotalSpent = g.Sum(x => x.Revenue)
            })
            .OrderByDescending(x => x.TotalSpent)
            .ThenByDescending(x => x.Orders)
            .Take(5)
            .ToList();
    }

    private static string BuildExcel(AnalyticsViewModel vm)
    {
        var html = new StringBuilder();
        html.AppendLine("<html><head><meta charset='utf-8' /></head><body>");
        html.AppendLine("<h2>Analytics Report</h2>");
        html.AppendLine($"<p>Period: {EscapeHtml(vm.PeriodType)} | Range: {vm.RangeStart:yyyy-MM-dd} to {vm.RangeEndExclusive.AddDays(-1):yyyy-MM-dd}</p>");

        html.AppendLine("<table border='1' cellspacing='0' cellpadding='4'>");
        html.AppendLine("<tr><th>Revenue</th><th>Orders</th><th>New Users</th><th>Return Requests</th><th>Reviews</th><th>Avg Rating</th></tr>");
        html.AppendLine($"<tr><td>{vm.TotalRevenue.ToString("0.##", CultureInfo.InvariantCulture)}</td><td>{vm.TotalOrders}</td><td>{vm.NewUsers}</td><td>{vm.TotalReturns}</td><td>{vm.TotalReviews}</td><td>{vm.AverageRating.ToString("0.##", CultureInfo.InvariantCulture)}</td></tr>");
        html.AppendLine("</table><br/>");

        html.AppendLine("<h3>Top Sellers (by revenue)</h3>");
        html.AppendLine("<table border='1' cellspacing='0' cellpadding='4'><tr><th>Seller</th><th>Orders</th><th>Items</th><th>Revenue</th></tr>");
        foreach (var row in vm.TopSellers)
        {
            html.AppendLine($"<tr><td>{EscapeHtml(row.Seller)}</td><td>{row.Orders}</td><td>{row.Items}</td><td>{row.Revenue.ToString("0.##", CultureInfo.InvariantCulture)}</td></tr>");
        }
        if (vm.TopSellers.Count == 0)
        {
            html.AppendLine("<tr><td colspan='4'>No seller data.</td></tr>");
        }
        html.AppendLine("</table><br/>");

        html.AppendLine("<h3>Top Products (by quantity)</h3>");
        html.AppendLine("<table border='1' cellspacing='0' cellpadding='4'><tr><th>Product</th><th>Qty</th><th>Revenue</th></tr>");
        foreach (var row in vm.TopProducts)
        {
            html.AppendLine($"<tr><td>{EscapeHtml(row.Product)}</td><td>{row.Quantity}</td><td>{row.Revenue.ToString("0.##", CultureInfo.InvariantCulture)}</td></tr>");
        }
        if (vm.TopProducts.Count == 0)
        {
            html.AppendLine("<tr><td colspan='3'>No product data.</td></tr>");
        }
        html.AppendLine("</table><br/>");

        html.AppendLine("<h3>Top Buyers (by spending)</h3>");
        html.AppendLine("<table border='1' cellspacing='0' cellpadding='4'><tr><th>Buyer</th><th>Orders</th><th>Total spent</th></tr>");
        foreach (var row in vm.TopBuyers)
        {
            html.AppendLine($"<tr><td>{EscapeHtml(row.Buyer)}</td><td>{row.Orders}</td><td>{row.TotalSpent.ToString("0.##", CultureInfo.InvariantCulture)}</td></tr>");
        }
        if (vm.TopBuyers.Count == 0)
        {
            html.AppendLine("<tr><td colspan='3'>No buyer data.</td></tr>");
        }
        html.AppendLine("</table>");

        html.AppendLine("</body></html>");
        return html.ToString();
    }

    private static string EscapeHtml(string value)
        => System.Net.WebUtility.HtmlEncode(value);

    private bool HasAdminAccess()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var role = HttpContext.Session.GetString("Role");
        var isAdminTwoFactorVerified = HttpContext.Session.GetString("IsAdmin2FAVerified");

        return userId is not null
            && AllowedRoles.Contains(role ?? string.Empty)
            && string.Equals(isAdminTwoFactorVerified, "true", StringComparison.OrdinalIgnoreCase);
    }
}
