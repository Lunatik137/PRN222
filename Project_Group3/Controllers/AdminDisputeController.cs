using Microsoft.AspNetCore.Mvc;
using Project_Group3.Repository.Interfaces;

namespace Project_Group3.Controllers;

public class AdminDisputeController(IDisputeRepository repo) : Controller
{
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "superadmin",
        "monitor"
    };

    public IActionResult IndexDispute()
    {
        if (!HasAdminAccess())
        {
            return RedirectToAction("Login", "Account");
        }

        var data = repo.GetAll();
        return View("IndexDispute", data);
    }

    public IActionResult Details(int id)
    {
        if (!HasAdminAccess())
        {
            return RedirectToAction("Login", "Account");
        }

        var dispute = repo.GetById(id);
        return View(dispute);
    }

    public IActionResult Edit(int id)
    {
        if (!HasAdminAccess())
        {
            return RedirectToAction("Login", "Account");
        }

        var dispute = repo.GetById(id);
        return View(dispute);
    }

    [HttpPost]
    public IActionResult Update(int id, string status, string resolution)
    {
        if (!HasAdminAccess())
        {
            return RedirectToAction("Login", "Account");
        }

        var dispute = repo.GetById(id);
        if (dispute.status == "RESOLVED")
        {
            return RedirectToAction("Details", new { id });
        }

        repo.Update(id, status, resolution);
        return RedirectToAction("Details", new { id });
    }

    private bool HasAdminAccess()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var role = HttpContext.Session.GetString("Role");
        var isAdminTwoFactorVerified = HttpContext.Session.GetString("IsAdmin2FAVerified");

        return userId is not null
            && AllowedRoles.Contains(role ?? string.Empty)
            && string.Equals(isAdminTwoFactorVerified, "true", StringComparison.OrdinalIgnoreCase);
    }
}