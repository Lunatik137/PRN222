using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_Group3.Models;
using Project_Group3.Security;

namespace Project_Group3.Controllers;

public class EmailController(CloneEbayDbContext dbContext, IConfiguration configuration, ILogger<EmailController> logger) : Controller
{
    [HttpGet]
    public IActionResult Send()
    {
        if (!HasAdminAccess())
        {
            return RedirectToAction("Login", "Account");
        }

        ViewBag.Roles = GetAvailableTargetRoles();
        ViewBag.Recipients = new List<User>();
        ViewBag.RecipientCount = 0;
        return View(new EmailRequest());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(EmailRequest request, string actionType = "Send", CancellationToken cancellationToken = default)
    {
        if (!HasAdminAccess())
        {
            return RedirectToAction("Login", "Account");
        }

        var availableTargetRoles = GetAvailableTargetRoles();
        ViewBag.Roles = availableTargetRoles;

        if (string.IsNullOrWhiteSpace(request.ToRole)
            || !availableTargetRoles.Contains(request.ToRole.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            TempData["Message"] = "You do not have permission to send email to that target group.";
            TempData["MessageType"] = "error";
            ViewBag.Recipients = new List<User>();
            ViewBag.RecipientCount = 0;
            return View(request);
        }

        var recipients = await GetRecipientsByRoleAsync(request.ToRole.Trim(), cancellationToken);
        ViewBag.Recipients = recipients;
        ViewBag.RecipientCount = recipients.Count;

        if (string.Equals(actionType, "Preview", StringComparison.OrdinalIgnoreCase))
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
            TempData["Message"] = "Vui lòng nhập nội dung email.";
            TempData["MessageType"] = "error";
            return View(request);
        }

        if (!recipients.Any())
        {
            TempData["Message"] = $"Không có người dùng nào khớp nhóm: {request.ToRole}";
            TempData["MessageType"] = "error";
            return View(request);
        }

        var emails = recipients
            .Where(x => !string.IsNullOrWhiteSpace(x.email))
            .Select(x => x.email!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var sent = await SendBulkEmailAsync(emails, request.Subject.Trim(), request.Body);
        if (sent)
        {
            TempData["Message"] = $"Đã gửi thông báo tới {emails.Count} địa chỉ email.";
            TempData["MessageType"] = "success";
            return RedirectToAction(nameof(Send));
        }

        TempData["Message"] = "Gửi email thất bại. Kiểm tra cấu hình EmailSettings.";
        TempData["MessageType"] = "error";
        return View(request);
    }

    private async Task<List<User>> GetRecipientsByRoleAsync(string role, CancellationToken cancellationToken)
    {
        var query = dbContext.Users
            .AsNoTracking()
            .Where(x => !string.IsNullOrWhiteSpace(x.email));

        if (!string.Equals(role, "All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x => x.role != null && x.role.ToLower() == role.ToLower());
        }

        return await query
            .OrderBy(x => x.username)
            .ToListAsync(cancellationToken);
    }

    private async Task<bool> SendBulkEmailAsync(List<string> toEmails, string subject, string body)
    {
        try
        {
            var simulate = configuration.GetValue<bool>("EmailSettings:SimulateEmailSending");
            if (simulate)
            {
                foreach (var to in toEmails)
                {
                    logger.LogInformation("[SimulateEmail] To={To} | Subject={Subject}", to, subject);
                }
                return true;
            }

            var host = configuration["EmailSettings:SmtpHost"];
            var port = configuration.GetValue<int?>("EmailSettings:SmtpPort") ?? 587;
            var user = configuration["EmailSettings:SmtpUser"];
            var password = configuration["EmailSettings:SmtpPassword"];
            var senderEmail = configuration["EmailSettings:SenderEmail"];
            var senderName = configuration["EmailSettings:SenderName"] ?? "System Notification";

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(senderEmail))
            {
                logger.LogWarning("EmailSettings chưa cấu hình đầy đủ để gửi email thật.");
                return false;
            }

            using var smtp = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(user, password),
                EnableSsl = true
            };

            foreach (var to in toEmails)
            {
                using var mail = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mail.To.Add(to);
                await smtp.SendMailAsync(mail);
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Gửi email hàng loạt thất bại");
            return false;
        }
    }

    private bool HasAdminAccess()
        => HttpContext.HasAdminPermission(AdminPermissions.CanAccessEmailSystem);

    private List<string> GetAvailableTargetRoles()
        => AdminPermissions.GetAllowedEmailTargets(HttpContext.GetCurrentRole()).ToList();
}
