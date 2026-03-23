using Microsoft.AspNetCore.Mvc;
using Project_Group3.Models;
using Project_Group3.Repository.Interfaces;

namespace Project_Group3.Controllers;
public class AccountController(IUserRepository userRepository) : Controller
{
    private static readonly HashSet<string> AdminRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "superadmin",
        "monitor"
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

            if (!AdminRoles.Contains(user.role ?? string.Empty))
            {
                ModelState.AddModelError(string.Empty, "You do not have permission to access the admin portal.");
                return View(model);
            }

            HttpContext.Session.SetInt32("UserId", user.id);
            HttpContext.Session.SetString("Username", user.username ?? model.Username);
            HttpContext.Session.SetString("Role", user.role ?? string.Empty);

            TempData["LoginMessage"] = $"Hello, {user.username}!";
            return RedirectToAction("Dashboard", "Admin");
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }
}