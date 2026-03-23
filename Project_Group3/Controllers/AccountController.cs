using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Project_Group3.Hubs;
using Project_Group3.Models;
using Project_Group3.Repository.Interfaces;

namespace Project_Group3.Controllers;
public class AccountController(
    IUserRepository userRepository,
    IHubContext<AdminNotificationHub> adminNotificationHub) : Controller
{
    private static readonly HashSet<string> AdminRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "superadmin",
        "monitor"
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

                TempData["LoginMessage"] = $"Hello, {user.username}!";
                return RedirectToAction("Index", "Home");
            }

            HttpContext.Session.SetInt32("UserId", user.id);
            HttpContext.Session.SetString("Username", user.username ?? model.Username);
            HttpContext.Session.SetString("Role", user.role ?? string.Empty);

            TempData["LoginMessage"] = $"Hello, {user.username}!";
            return RedirectToAction("Dashboard", "AdminDashboard");
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
}
