using Microsoft.AspNetCore.Mvc;
using Project_Group3.Repository.Interfaces;
using Project_Group3.Security;

namespace Project_Group3.Controllers;

public class AdminDisputeController(IDisputeRepository repo) : Controller
{
    public IActionResult IndexDispute()
    {
        if (!CanAccessDisputes())
        {
            return RedirectToAction("Login", "Account");
        }

        var data = repo.GetAll();
        return View("IndexDispute", data);
    }

    public IActionResult Details(int id)
    {
        if (!CanAccessDisputes())
        {
            return RedirectToAction("Login", "Account");
        }

        var dispute = repo.GetById(id);
        return View(dispute);
    }

    public IActionResult Edit(int id)
    {
        if (!CanProcessDisputes())
        {
            return RedirectToAction("Login", "Account");
        }

        var dispute = repo.GetById(id);
        return View(dispute);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Update(int id, string status, string resolution)
    {
        if (!CanProcessDisputes())
        {
            return RedirectToAction("Login", "Account");
        }

        var dispute = repo.GetById(id);
        if (dispute is null)
        {
            return NotFound();
        }

        if (!CanFinalizeDisputes()
            && (string.Equals(status, "RESOLVED", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "REJECTED", StringComparison.OrdinalIgnoreCase)))
        {
            TempData["Message"] = "Only superadmin can make the final dispute decision.";
            TempData["MessageType"] = "error";
            return RedirectToAction("Details", new { id });
        }

        if (string.Equals(dispute.status, "RESOLVED", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction("Details", new { id });
        }

        repo.Update(id, status, resolution);
        return RedirectToAction("Details", new { id });
    }

    private bool CanAccessDisputes()
        => HttpContext.HasAdminPermission(AdminPermissions.CanAccessDisputes);

    private bool CanProcessDisputes()
        => HttpContext.HasAdminPermission(AdminPermissions.CanProcessDisputes);

    private bool CanFinalizeDisputes()
        => HttpContext.HasAdminPermission(AdminPermissions.CanFinalizeDisputes);
}
