using DotNetEnv;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRN222_Group3.Models;
using PRN222_Group3.Repository;
using System.Text.Json;
namespace PRN222_Group3.Controllers
{
    public class CustomerController : Controller
    {
        private CloneEbayDbContext _context;
        private readonly IWebHostEnvironment _env;

        public CustomerController(CloneEbayDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            var buyerIdStr = HttpContext.Session.GetString("Id");
            Console.WriteLine(buyerIdStr);
            if (string.IsNullOrEmpty(buyerIdStr))
                return RedirectToAction("Login", "Login");

            int buyerId = int.Parse(buyerIdStr);

            var query = _context.OrderTables
                                .Where(o => o.BuyerId == buyerId)
                                .OrderByDescending(o => o.OrderDate)
                                .Include(o => o.OrderItems)
                                    .ThenInclude(oi => oi.Product);

            int totalOrders = await query.CountAsync();

            var orders = await query.Skip((page - 1) * pageSize)
                                    .Take(pageSize)
                                    .ToListAsync();

            // Lấy ReturnRequest cho các đơn hàng này
            var returnRequests = await _context.ReturnRequests
                                               .Where(r => r.UserId == buyerId && orders.Select(o => o.Id).Contains(r.OrderId.Value))
                                               .ToListAsync();

            // Truyền sang View qua ViewData hoặc ViewModel
            ViewData["ReturnRequests"] = returnRequests;

            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = (int)Math.Ceiling((double)totalOrders / pageSize);

            return View(orders);
        }

        [HttpPost]
       
        public async Task<IActionResult> RequestCancel(int OrderId, string Reason, List<IFormFile> Files)
        {
            var userIdStr = HttpContext.Session.GetString("Id");
            if (string.IsNullOrEmpty(userIdStr))
                return RedirectToAction("Login", "Login");

            int userId = int.Parse(userIdStr);

            // Lấy ReturnRequest hiện tại nếu đã tồn tại cho order này
            var returnRequest = await _context.ReturnRequests
                                              .FirstOrDefaultAsync(r => r.OrderId == OrderId && r.UserId == userId);

            if (returnRequest == null)
            {
                returnRequest = new ReturnRequest
                {
                    OrderId = OrderId,
                    UserId = userId,
                    Reason = Reason.Trim(),
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };
                _context.ReturnRequests.Add(returnRequest);
            }
            else
            {
                // Cập nhật lý do nếu cần
                returnRequest.Reason = Reason;
            }

            // Lưu file mới (nếu có)
            if (Files != null && Files.Count > 0)
            {
                var savedFiles = new List<string>();
                var uploadFolder = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

                foreach (var file in Files)
                {
                    if (file.Length > 0)
                    {
                        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                        var filePath = Path.Combine(uploadFolder, fileName);
                        using var stream = new FileStream(filePath, FileMode.Create);
                        await file.CopyToAsync(stream);
                        savedFiles.Add(fileName);
                    }
                }

                // Gộp file cũ + file mới
                var existingFiles = string.IsNullOrEmpty(returnRequest.Images)
                                    ? new List<string>()
                                    : returnRequest.Images.Split(';').ToList();
                existingFiles.AddRange(savedFiles);
                returnRequest.Images = string.Join(";", existingFiles);
            }
            var order = await _context.OrderTables.FindAsync(OrderId);
            if (order != null)
            {
                order.Status = "ReturnRequested"; 
            }
            await _context.SaveChangesAsync();
            TempData["Message"] = "Yêu cầu hủy đơn hàng đã được cập nhật!";
            return RedirectToAction("Index");
        }

    }
}
