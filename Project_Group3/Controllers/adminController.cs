using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRN222_Group3.Models;
using PRN222_Group3.Views.ViewModel;

namespace PRN222_Group3.Controllers
{
    public class AdminController : Controller
    {
        private readonly CloneEbayDbContext _context;

        public AdminController(CloneEbayDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var dashboard = new DashboardViewModel
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalProducts = await _context.Products.CountAsync(),
                TotalOrders = await _context.OrderTables.CountAsync(),
                TotalRevenue = await _context.OrderTables
                    .Where(p => p.Status == "Completed")
                    .SumAsync(p => (int?)p.TotalPrice) ?? 0,
                RecentProducts = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Seller)
                    .OrderByDescending(p => p.Id)
                    .Take(10)
                    .ToListAsync(),
                RecentOrders = await _context.OrderTables
                    .Include(o => o.Buyer)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(10)
                    .ToListAsync()
            };

            return View(dashboard);
        }

    }
}
