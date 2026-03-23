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

        var vm = new DashboardViewModel
        {
            TotalUsers = await dbContext.Users.CountAsync(cancellationToken),
            TotalProducts = await dbContext.Products.CountAsync(cancellationToken),
            TotalOrders = await dbContext.OrderTables.CountAsync(cancellationToken)
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
}