using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Project_Group3.Hubs;
using Project_Group3.Models;
using Project_Group3.Repository.Interfaces;

namespace Project_Group3.Controllers;
public class AccountController(
    IUserRepository userRepository,
    IHubContext<AdminNotificationHub> adminNotificationHub,
    IConfiguration configuration,
    ILogger<AccountController> logger) : Controller
{
    private const string PendingAdminUserIdSessionKey = "PendingAdminUserId";
    private const string PendingAdminUsernameSessionKey = "PendingAdminUsername";
    private const string PendingAdminRoleSessionKey = "PendingAdminRole";
    private const string AdminTwoFactorVerifiedSessionKey = "IsAdmin2FAVerified";

    private static readonly HashSet<string> AdminRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "superadmin",
        "monitor",
        "support"
    };

    private static readonly HashSet<string> AllowedRegisterRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Buyer",
        "Seller"
    };

    [HttpGet]
    public IActionResult Login()
    {
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var user = await userRepository.GetByCredentialsAsync(
                model.Username,
                model.Password,
                CancellationToken.None
            );
            if (user is null)
            {
                ModelState.AddModelError(string.Empty, "Username or password is not correct.");
                return View(model);
            }

            if (user.isLocked)
            {
                ModelState.AddModelError(string.Empty, "Your account has been locked.");
                return View(model);
            }

            if (!user.isApproved)
            {
                ModelState.AddModelError(string.Empty, "Your account is pending admin approval.");
                return View(model);
            }

            if (!AdminRoles.Contains(user.role ?? string.Empty))
            {
                HttpContext.Session.SetInt32("UserId", user.id);
                HttpContext.Session.SetString("Username", user.username ?? model.Username);
                HttpContext.Session.SetString("Role", user.role ?? string.Empty);
                HttpContext.Session.Remove(AdminTwoFactorVerifiedSessionKey);

                TempData["LoginMessage"] = $"Hello, {user.username}!";
                return RedirectToAction("Index", "Home");
            }

            if (user.isTwoFactorEnabled != true)
            {
                ModelState.AddModelError(string.Empty, "Admin account must enable 2FA before accessing Admin panel.");
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(user.email))
            {
                ModelState.AddModelError(string.Empty, "Your account does not have an email. Please contact superadmin.");
                return View(model);
            }

            user.twoFactorSecret = GenerateTwoFactorCode();
            var savedCode = await userRepository.UpdateUserAsync(user, CancellationToken.None);
            if (!savedCode)
            {
                ModelState.AddModelError(string.Empty, "Cannot create 2FA verification code. Please try again.");
                return View(model);
            }

            var mailSent = await SendTwoFactorCodeEmailAsync(user.email, user.username ?? model.Username, user.twoFactorSecret);
            if (!mailSent)
            {
                ModelState.AddModelError(string.Empty, "Cannot send 2FA code to your email. Please try again later.");
                return View(model);
            }

            SetPendingAdminTwoFactorSession(user, model.Username);
            return RedirectToAction(nameof(VerifyAdmin2FA));
        }
        catch (TaskCanceledException)
        {
            ModelState.AddModelError(string.Empty, "The login request has been canceled. Please try again.");
            return View(model);
        }
        catch (Exception)
        {
            ModelState.AddModelError(string.Empty, "An error occurred during the login process.");
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult VerifyAdmin2FA()
    {
        if (!HasPendingAdminTwoFactorSession())
        {
            return RedirectToAction(nameof(Login));
        }

        return View(new AdminTwoFactorViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyAdmin2FA(AdminTwoFactorViewModel model)
    {
        if (!HasPendingAdminTwoFactorSession())
        {
            return RedirectToAction(nameof(Login));
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var pendingAdminUserId = HttpContext.Session.GetInt32(PendingAdminUserIdSessionKey);
        if (pendingAdminUserId is null)
        {
            return RedirectToAction(nameof(Login));
        }

        var user = await userRepository.GetByIdAsync(pendingAdminUserId.Value, CancellationToken.None);
        if (user is null || !AdminRoles.Contains(user.role ?? string.Empty))
        {
            ClearPendingAdminTwoFactorSession();
            return RedirectToAction(nameof(Login));
        }

        if (!string.Equals(user.twoFactorSecret?.Trim(), model.Code.Trim(), StringComparison.Ordinal))
        {
            ModelState.AddModelError(nameof(model.Code), "2FA code is not valid.");
            return View(model);
        }

        HttpContext.Session.SetInt32("UserId", user.id);
        HttpContext.Session.SetString("Username", user.username ?? HttpContext.Session.GetString(PendingAdminUsernameSessionKey) ?? string.Empty);
        HttpContext.Session.SetString("Role", user.role ?? HttpContext.Session.GetString(PendingAdminRoleSessionKey) ?? string.Empty);
        HttpContext.Session.SetString(AdminTwoFactorVerifiedSessionKey, "true");
        user.twoFactorSecret = null;
        await userRepository.UpdateUserAsync(user, CancellationToken.None);

        ClearPendingAdminTwoFactorSession();

        TempData["LoginMessage"] = $"Hello, {user.username}!";
        return string.Equals(user.role, "superadmin", StringComparison.OrdinalIgnoreCase)
               || string.Equals(user.role, "monitor", StringComparison.OrdinalIgnoreCase)
            ? RedirectToAction("Dashboard", "AdminDashboard")
            : RedirectToAction("SystemSettings", "Admin");
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!AllowedRegisterRoles.Contains(model.Role))
        {
            ModelState.AddModelError(nameof(model.Role), "Selected account type is not valid.");
            return View(model);
        }

        var existingEmail = await userRepository.GetByEmailAsync(model.Email.Trim(), CancellationToken.None);
        if (existingEmail is not null)
        {
            ModelState.AddModelError(nameof(model.Email), "Email already exists.");
            return View(model);
        }

        var existingUsername = await userRepository.GetByUsernameAsync(model.Username.Trim(), CancellationToken.None);
        if (existingUsername is not null)
        {
            ModelState.AddModelError(nameof(model.Username), "Username already exists.");
            return View(model);
        }

        var user = new User
        {
            username = model.Username.Trim(),
            email = model.Email.Trim(),
            password = model.Password,
            role = model.Role,
            Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim(),
            createdAt = DateTime.UtcNow,
            registrationIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
            isApproved = false,
            isLocked = false,
            isTwoFactorEnabled = false,
            RiskScore = 0,
            RiskLevel = "Low"
        };

        var created = await userRepository.CreateUserAsync(user, CancellationToken.None);
        if (!created)
        {
            ModelState.AddModelError(string.Empty, "Unable to create account. Please try again.");
            return View(model);
        }

        await adminNotificationHub.Clients.Group(AdminNotificationHub.AdminGroupName).SendAsync(
            "UserRegistered",
            user.username,
            user.email,
            user.createdAt);

        TempData["LoginMessage"] = "Register successful. Your account is pending admin approval.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }

    private void SetPendingAdminTwoFactorSession(User user, string usernameFallback)
    {
        HttpContext.Session.Remove("UserId");
        HttpContext.Session.Remove("Username");
        HttpContext.Session.Remove("Role");

        HttpContext.Session.SetInt32(PendingAdminUserIdSessionKey, user.id);
        HttpContext.Session.SetString(PendingAdminUsernameSessionKey, user.username ?? usernameFallback);
        HttpContext.Session.SetString(PendingAdminRoleSessionKey, user.role ?? string.Empty);
        HttpContext.Session.Remove(AdminTwoFactorVerifiedSessionKey);
    }

    private bool HasPendingAdminTwoFactorSession()
        => HttpContext.Session.GetInt32(PendingAdminUserIdSessionKey) is not null;

    private void ClearPendingAdminTwoFactorSession()
    {
        HttpContext.Session.Remove(PendingAdminUserIdSessionKey);
        HttpContext.Session.Remove(PendingAdminUsernameSessionKey);
        HttpContext.Session.Remove(PendingAdminRoleSessionKey);
    }

    private static string GenerateTwoFactorCode()
        => RandomNumberGenerator.GetInt32(100000, 999999).ToString();

    private async Task<bool> SendTwoFactorCodeEmailAsync(string toEmail, string username, string code)
    {
        try
        {
            var simulate = configuration.GetValue<bool>("EmailSettings:SimulateEmailSending");
            if (simulate)
            {
                logger.LogInformation("[Simulate2FA] To={To} | Username={Username} | Code={Code}", toEmail, username, code);
                return true;
            }

            var host = configuration["EmailSettings:SmtpHost"];
            var port = configuration.GetValue<int?>("EmailSettings:SmtpPort") ?? 587;
            var smtpUser = configuration["EmailSettings:SmtpUser"];
            var smtpPassword = configuration["EmailSettings:SmtpPassword"];
            var senderEmail = configuration["EmailSettings:SenderEmail"];
            var senderName = configuration["EmailSettings:SenderName"] ?? "CloneEbay System";

            if (string.IsNullOrWhiteSpace(host)
                || string.IsNullOrWhiteSpace(smtpUser)
                || string.IsNullOrWhiteSpace(smtpPassword)
                || string.IsNullOrWhiteSpace(senderEmail))
            {
                logger.LogWarning("EmailSettings are missing, cannot send admin 2FA code.");
                return false;
            }

            using var smtp = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPassword),
                EnableSsl = true
            };

            using var mail = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName),
                Subject = "CloneEbay Admin 2FA verification code",
                Body = $"Hello {WebUtility.HtmlEncode(username)},<br/>Your admin 2FA code is: <b>{WebUtility.HtmlEncode(code)}</b><br/>This code is required to complete login.",
                IsBodyHtml = true
            };
            mail.To.Add(toEmail);

            await smtp.SendMailAsync(mail);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send admin 2FA code to {ToEmail}", toEmail);
            return false;
        }
    }
}