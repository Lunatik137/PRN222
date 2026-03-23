using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRN222_Group3.Models;
using PRN222_Group3.Repository;
using PRN222_Group3.Service;

namespace PRN222_Group3.Controllers
{
    [Authorize(Policy = "ReturnAndSystemNotify")]
    public class EmailController : Controller
    {
        private static readonly List<string> EmailTargetRoles =
        [
            "All", "Buyer", "Seller", "SuperAdmin", "Moderator", "Monitor", "Support", "Ops"
        ];

        private readonly IEmailService _emailService;
        private readonly UserRepository _userRepository;

        public EmailController(IEmailService emailService, UserRepository userRepository)
        {
            _emailService = emailService;
            _userRepository = userRepository;
        }

        // GET: /Email/Send
        [HttpGet]
        public IActionResult Send()
        {
            ViewBag.Roles = EmailTargetRoles;
            return View();
        }

        // POST: /Email/Send
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(EmailRequest request, string actionType = "Send")
        {
            ViewBag.Roles = EmailTargetRoles;

            if (string.IsNullOrWhiteSpace(request.ToRole))
            {
                TempData["Message"] = "Vui lòng chọn nhóm người nhận.";
                TempData["MessageType"] = "error";
                return View(request);
            }

            var recipients = GetRecipientsByRole(request.ToRole);
            ViewBag.Recipients = recipients;
            ViewBag.RecipientCount = recipients.Count;

            if (actionType.Equals("Preview", StringComparison.OrdinalIgnoreCase))
            {
                if (!recipients.Any())
                {
                    TempData["Message"] = $"Không có người dùng nào khớp nhóm: {request.ToRole}";
                    TempData["MessageType"] = "error";
                }
                return View(request);
            }

            if (string.IsNullOrWhiteSpace(request.Subject))
            {
                TempData["Message"] = "Vui lòng nhập tiêu đề email.";
                TempData["MessageType"] = "error";
                return View(request);
            }

            if (string.IsNullOrWhiteSpace(request.Body))
            {
                TempData["Message"] = "Vui lòng nhập nội dung (cảnh báo / thông báo hệ thống).";
                TempData["MessageType"] = "error";
                return View(request);
            }

            if (!recipients.Any())
            {
                TempData["Message"] = $"Không có người dùng nào khớp nhóm: {request.ToRole}";
                TempData["MessageType"] = "error";
                return View(request);
            }

            try
            {
                var recipientEmails = recipients.Select(u => u.Email!).ToList();

                // Send emails
                var result = await _emailService.SendBulkEmailAsync(recipientEmails, request.Subject, request.Body);

                if (result)
                {
                    TempData["Message"] = $"Đã gửi thông báo tới {recipientEmails.Count} địa chỉ email (hoặc chế độ mô phỏng đang bật — xem log console).";
                    TempData["MessageType"] = "success";
                    return RedirectToAction("Send");
                }
                else
                {
                    TempData["Message"] = "Gửi email thất bại. Kiểm tra cấu hình SMTP hoặc bật SimulateEmailSending trong Development.";
                    TempData["MessageType"] = "error";
                }
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Lỗi khi gửi email: {ex.Message}";
                TempData["MessageType"] = "error";
            }

            return View(request);
        }

        private List<User> GetRecipientsByRole(string role)
        {
            var users = _userRepository.GetUsers();
            if (role == "All")
            {
                return users.Where(u => !string.IsNullOrEmpty(u.Email)).ToList();
            }
            return users.Where(u => u.Role == role && !string.IsNullOrEmpty(u.Email)).ToList();
        }
    }
}
