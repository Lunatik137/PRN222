using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Project_Group3.Hubs;
using Project_Group3.Models;

namespace Project_Group3.Controllers
{
    public class HomeController(
           CloneEbayDbContext dbContext,
           ILogger<HomeController> logger,
           IHubContext<AdminNotificationHub> adminNotificationHub) : Controller
    {
        private const string BuyerRole = "buyer";
        private const string ProductStatusActive = "active";
        private const string ProductStatusReported = "reported";
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var role = HttpContext.Session.GetString("Role") ?? string.Empty;
            var isBuyer = string.Equals(role, BuyerRole, StringComparison.OrdinalIgnoreCase);

            List<BuyerProductCardViewModel> products = [];
            if (isBuyer)
            {
                products = await dbContext.Products
                     .AsNoTracking()
                     .Include(p => p.category)
                     .Include(p => p.seller)
                     .Where(p => p.status != null &&
                         (p.status.ToLower() == ProductStatusActive || p.status.ToLower() == ProductStatusReported))
                     .OrderByDescending(p => p.id)
                     .Select(p => new BuyerProductCardViewModel
                     {
                        Id = p.id,
                        Title = string.IsNullOrWhiteSpace(p.title) ? $"Product #{p.id}" : p.title!,
                        Description = p.description,
                        Price = p.price,
                        ImageUrl = p.images,
                        CategoryName = p.category != null ? p.category.name : null,
                        SellerName = p.seller != null ? p.seller.username : null
                    })
                    .ToListAsync(cancellationToken);
            }

            var vm = new BuyerMarketplaceViewModel
            {
                IsBuyer = isBuyer,
                Products = products
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportProduct(BuyerReportProductInput input, CancellationToken cancellationToken)
        {
            var role = HttpContext.Session.GetString("Role") ?? string.Empty;
            if (!string.Equals(role, BuyerRole, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ActionError"] = "Only buyer accounts can report products.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(input.Reason))
            {
                TempData["ActionError"] = "Reason is required when reporting a product.";
                return RedirectToAction(nameof(Index));
            }

            var product = await dbContext.Products.FirstOrDefaultAsync(p => p.id == input.ProductId, cancellationToken);
            if (product is null)
            {
                TempData["ActionError"] = $"Product #{input.ProductId} was not found.";
                return RedirectToAction(nameof(Index));
            }

            var reportReason = input.Reason.Trim();
            var buyerName = HttpContext.Session.GetString("Username") ?? "Buyer";

            product.status = ProductStatusReported;
            product.reportnumber = (product.reportnumber ?? 0) + 1;
            product.reason = AppendReportReason(product.reason, reportReason, buyerName);
            await dbContext.SaveChangesAsync(cancellationToken);

            await adminNotificationHub.Clients.Group(AdminNotificationHub.AdminGroupName).SendAsync(
                "ProductReported",
                product.id,
                product.title ?? $"Product #{product.id}",
                reportReason,
                buyerName,
                DateTime.UtcNow,
                cancellationToken);

            TempData["ActionSuccess"] = $"Product #{product.id} has been moved to '{ProductStatusReported}' with reason: {reportReason}";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private static string AppendReportReason(string? existingReasons, string newReason, string reporter)
        {
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {reporter}: {newReason}"; return string.IsNullOrWhiteSpace(existingReasons)
                ? line
                : $"{existingReasons}{Environment.NewLine}{line}";
        }
    }
}