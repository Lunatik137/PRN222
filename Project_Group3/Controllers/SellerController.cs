using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRN222_Group3.Models;

namespace PRN222_Group3.Controllers
{
    public class SellerController : Controller
    {
        private readonly CloneEbayDbContext _context;
        private readonly IWebHostEnvironment _env;

        public SellerController(CloneEbayDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            var sellerIdStr = HttpContext.Session.GetString("Id");
            if (string.IsNullOrEmpty(sellerIdStr))
                return RedirectToAction("Login", "Login");

            int sellerId = int.Parse(sellerIdStr);

            // Lấy tất cả OrderItems có product của seller này
            var orderIds = await _context.OrderItems
                                .Where(oi => oi.Product.SellerId == sellerId)
                                .Select(oi => oi.OrderId)
                                .Distinct()
                                .ToListAsync();

            var query = _context.OrderTables
                                .Where(o => orderIds.Contains(o.Id))
                                .OrderByDescending(o => o.OrderDate)
                                .Include(o => o.OrderItems)
                                    .ThenInclude(oi => oi.Product);

            int totalOrders = await query.CountAsync();

            var orders = await query.Skip((page - 1) * pageSize)
                                    .Take(pageSize)
                                    .ToListAsync();

            // Lấy ReturnRequest nếu có
            var returnRequests = await _context.ReturnRequests
                                               .Where(r => orders.Select(o => o.Id).Contains(r.OrderId.Value))
                                               .ToListAsync();

            ViewData["ReturnRequests"] = returnRequests;
            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = (int)Math.Ceiling((double)totalOrders / pageSize);

            return View(orders);
        }

        /// <summary>
        /// Theo SRS (Nhóm 2 Seller): seller không xét duyệt hoàn trả — chỉ admin (ReturnRequestAdmin).
        /// Giữ action để request giả mạo không gây 404; không cập nhật DB.
        /// </summary>
        [HttpPost]
        public Task<IActionResult> HandleReturn(int returnRequestId, string actionType)
        {
            /*
            // --- CODE CŨ: seller chấp nhận/từ chối hoàn trả trực tiếp (không còn phù hợp SRS) ---
            public async Task<IActionResult> HandleReturn_OLD(int returnRequestId, string actionType)
            {
                var rr = await _context.ReturnRequests.FindAsync(returnRequestId);
                if (rr == null) return NotFound();

                var order = await _context.OrderTables.FindAsync(rr.OrderId);
                if (order == null) return NotFound();

                if (actionType == "accept")
                {
                    rr.Status = "Completed";
                    order.Status = "Returned";
                }
                else if (actionType == "reject")
                {
                    // rr.Status = "Rejected";
                }

                await _context.SaveChangesAsync();
                TempData["Message"] = $"Yêu cầu hủy đơn hàng #{order.Id} đã được {(actionType == "accept" ? "chấp nhận" : "từ chối")}.";
                return RedirectToAction("Index");
            }
            */

            TempData["Message"] = "Theo quy định hệ thống, chỉ quản trị viên được duyệt hoặc từ chối yêu cầu hoàn trả đơn hàng.";
            return Task.FromResult<IActionResult>(RedirectToAction(nameof(Index)));
        }

        [HttpPost]
        public async Task<IActionResult> RejectCancel(int OrderId, string Description, List<IFormFile>? EvidenceFiles)
        {
            // Lưu ảnh
            string fileNames = "";
            if (EvidenceFiles != null)
            {
                foreach (var file in EvidenceFiles)
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                    var savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/", fileName);

                    using (var stream = new FileStream(savePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    fileNames += fileName + ";";
                }
            }
            var userIdStr = HttpContext.Session.GetString("Id");
            if (string.IsNullOrEmpty(userIdStr))
                return RedirectToAction("Login", "Login");

            int userId = int.Parse(userIdStr);
            // Tạo bản ghi Dispute
            var dispute = new Dispute
            {
                OrderId = OrderId,
                Description = Description,
                Resolution = fileNames,
                Status = "Open",
                RaisedBy = userId // Seller
            };

            _context.Disputes.Add(dispute);

            // Cập nhật trạng thái order
            

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

    }
}
