using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRN222_Group3.Models;
using PRN222_Group3.Repository;
using PRN222_Group3.Service;

namespace PRN222_Group3.Controllers
{
    [Authorize(Policy = "UserManageWrite")]
    public class EmailController : Controller
    {
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
            // Prepare roles for dropdown
            ViewBag.Roles = new List<string> { "All", "SuperAdmin", "Moderator", "Support", "Ops" };
            return View();
        }

        // POST: /Email/Send
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(EmailRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Subject))
            {
                TempData["Message"] = "Email subject is required.";
                TempData["MessageType"] = "error";
                ViewBag.Roles = new List<string> { "All", "SuperAdmin", "Moderator", "Support", "Ops" };
                return View(request);
            }

            if (string.IsNullOrWhiteSpace(request.Body))
            {
                TempData["Message"] = "Email content is required.";
                TempData["MessageType"] = "error";
                ViewBag.Roles = new List<string> { "All", "SuperAdmin", "Moderator", "Support", "Ops" };
                return View(request);
            }

            if (string.IsNullOrWhiteSpace(request.ToRole))
            {
                TempData["Message"] = "Please select a recipient role.";
                TempData["MessageType"] = "error";
                ViewBag.Roles = new List<string> { "All", "SuperAdmin", "Moderator", "Support", "Ops" };
                return View(request);
            }

            try
            {
                // Get users based on selected role
                var users = _userRepository.GetUsers();
                List<string> recipientEmails = new List<string>();

                if (request.ToRole == "All")
                {
                    recipientEmails = users
                        .Where(u => !string.IsNullOrEmpty(u.Email))
                        .Select(u => u.Email!)
                        .ToList();
                }
                else
                {
                    recipientEmails = users
                        .Where(u => u.Role == request.ToRole && !string.IsNullOrEmpty(u.Email))
                        .Select(u => u.Email!)
                        .ToList();
                }

                if (!recipientEmails.Any())
                {
                    TempData["Message"] = $"No users found with role: {request.ToRole}";
                    TempData["MessageType"] = "error";
                    ViewBag.Roles = new List<string> { "All", "SuperAdmin", "Moderator", "Support", "Ops" };
                    return View(request);
                }

                // Send emails
                var result = await _emailService.SendBulkEmailAsync(recipientEmails, request.Subject, request.Body);

                if (result)
                {
                    TempData["Message"] = $"Email sent successfully to {recipientEmails.Count} user(s)!";
                    TempData["MessageType"] = "success";
                    return RedirectToAction("Send");
                }
                else
                {
                    TempData["Message"] = "Failed to send email. Please check email configuration.";
                    TempData["MessageType"] = "error";
                }
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Error sending email: {ex.Message}";
                TempData["MessageType"] = "error";
            }

            ViewBag.Roles = new List<string> { "All", "SuperAdmin", "Moderator", "Support", "Ops" };
            return View(request);
        }

        // GET: /Email/Preview - Preview recipients
        [HttpGet]
        public IActionResult GetRecipients(string role)
        {
            var users = _userRepository.GetUsers();
            List<User> recipients;

            if (role == "All")
            {
                recipients = users.Where(u => !string.IsNullOrEmpty(u.Email)).ToList();
            }
            else
            {
                recipients = users.Where(u => u.Role == role && !string.IsNullOrEmpty(u.Email)).ToList();
            }

            return Json(new
            {
                count = recipients.Count,
                users = recipients.Select(u => new { u.Username, u.Email, u.Role })
            });
        }
    }
}
