using System.Net;
using System.Net.Mail;
using System.Text.Json;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRN222_Group3.Repository;
using PRN222_Group3.Service;
namespace PRN222_Group3.Controllers
{
    public class StoresController : Controller
    {
        private readonly ILogger<StoresController> _logger;
        private readonly StoresRepository _storesRepository;
        private readonly IEmailService _emailService;

        public StoresController(ILogger<StoresController> logger, StoresRepository storesRepository, IEmailService emailService)
        {
            _logger = logger;
            _storesRepository = storesRepository; // Gán từ DI
            _emailService = emailService;
        }

        // Sửa page = 3 thành page = 1
        public async Task<IActionResult> Stores(string searchString, int page = 1, int pageSize = 10)
        {
            var (stores, totalCount) = await _storesRepository.GetPagedStoresAsync(searchString, page, pageSize);
            ViewBag.Total = totalCount;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.CurrentSearch = searchString;
            return View(stores);
        }
        [Authorize(Policy = "UserManageWrite")]
        public async Task<IActionResult> Details(int id, string statusFilter, int page = 1)
        {
            // 2. Đặt pageSize cố định là 10
            const int pageSize = 10;

            // 3. Gọi Repository với các tham số mới
            var viewModel = await _storesRepository.GetStoreDetailsWithOrdersAsync(id, statusFilter, page, pageSize);

            if (viewModel == null)
            {
                return NotFound();
            }

            // 4. Gửi filter hiện tại về View để hiển thị trên dropdown
            ViewBag.CurrentStatusFilter = statusFilter;

            return View(viewModel); // Trả về dynamic model
        }

        [HttpGet]
        public async Task<IActionResult> OverdateRequestAsync()
        {
            var OverdateList = await _storesRepository.FindOverDateRequest();

            return View(OverdateList);
        }

        [HttpPost]
        public async Task<IActionResult> AdjustRequest(int Oid, int Rid, string status)
        {
            var result = await _storesRepository.UpdateOrderStatusAsync(Oid, status); // unknown

            var returnResult = await _storesRepository.UpdateReturnRequestStatusByOrderIdAsync(Rid, "Completed");

            var returnRequest = await _storesRepository.GetReturnRequestByOrderIdAsync(Rid);

            bool sent = await _emailService.SendEmailAsync(returnRequest.User.Email, "Your return request have adjust: " + status, "Your Requrn Request about" + returnRequest.Order.Id);

            return RedirectToAction("OverdateRequest");
        }

        [HttpPost]
        public async Task<IActionResult> SendEmail(string emailTo, string subject, string body)
        {
            bool sent = await _emailService.SendEmailAsync(emailTo, subject, body);

            if (sent)
            {
                TempData["msg"] = "Email đã được gửi thành công!";
            }
            else
            {
                TempData["msg"] = "Gửi email thất bại!";
            }

            return RedirectToAction("OverdateRequest");
        }

        [HttpGet]
        public async Task<IActionResult> GetReturnDetails(int orderId)
        {
            // Lấy thông tin ReturnRequest
            var returnRequest = await _storesRepository.GetReturnRequestByOrderIdAsync(orderId);

            // Lấy trạng thái của đơn hàng gốc
            var order = await _storesRepository.GetOrderByIdAsync(orderId);

            if (order == null)
            {
                return NotFound();
            }

            // Trả về JSON cho JavaScript
            return Json(new
            {
                ReturnRequestData = returnRequest, // Trả cả object ReturnRequest
                OrderStatus = order.Status
            });
        }

        // 2. ACTION CHẤP NHẬN HỦY (AJAX POST)
        [HttpPost]
        public async Task<IActionResult> AcceptCancel(int orderId)
        {
            // Cập nhật trạng thái đơn hàng thành "Cancelled"
            var success = await _storesRepository.UpdateOrderStatusAsync(orderId, "Cancelled");

            // (Tùy chọn: Bạn có thể cập nhật cả status của ReturnRequest)
            // var returnReq = ...; returnReq.Status = "Approved"; ...

            if (success)
            {
                await _storesRepository.UpdateReturnRequestStatusByOrderIdAsync(orderId, "Completed");
                return Json(new { success = true, message = "Đã duyệt hủy đơn hàng." });
            }
            return Json(new { success = false, message = "Lỗi: Không tìm thấy đơn hàng." });
        }

        // 3. ACTION TỪ CHỐI HỦY (AJAX POST)
        [HttpPost]
        public async Task<IActionResult> RejectCancel(int orderId)
        {
            // --- LƯU Ý LOGIC ---
            // Bạn yêu cầu update là "Completed".
            // Về mặt logic, điều này có thể không đúng. Khi từ chối
            // yêu cầu hủy, đơn hàng nên quay lại trạng thái
            // *trước đó* (ví dụ: "Processing" hoặc "Shipped") để
            // tiếp tục xử lý.
            // Tuy nhiên, tôi sẽ code theo đúng yêu cầu của bạn là "Completed".
            var success = await _storesRepository.UpdateOrderStatusAsync(orderId, "Completed");

            if (success)
            {
                return Json(new { success = true, message = "Đã từ chối yêu cầu hủy." });
            }
            return Json(new { success = false, message = "Lỗi: Không tìm thấy đơn hàng." });
        }

    }
}
