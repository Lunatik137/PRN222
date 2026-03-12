using Microsoft.AspNetCore.Mvc;

namespace Project_Group3.Controllers;

public class AdminController : Controller
{
    [HttpGet]
    public IActionResult Dashboard()
    {
        if (HttpContext.Session.GetInt32("UserId") is null)
        {
            return RedirectToAction("Login", "Account");
        }

        return View();
    }

    [HttpGet]
    public IActionResult UserManagement() => AdminSection("User Management");

    [HttpGet]
    public IActionResult ProductModeration() => AdminSection("Product Moderation");

    [HttpGet]
    public IActionResult OrderManagement() => AdminSection("Order Management");

    [HttpGet]
    public IActionResult ReviewsFeedback() => AdminSection("Reviews & Feedback");

    [HttpGet]
    public IActionResult ComplaintsDisputes() => AdminSection("Complaints / Disputes");

    [HttpGet]
    public IActionResult Analytics() => AdminSection("Analytics");

    [HttpGet]
    public IActionResult SystemSettings() => AdminSection("System Settings");

    private IActionResult AdminSection(string sectionTitle)
    {
        if (HttpContext.Session.GetInt32("UserId") is null)
        {
            return RedirectToAction("Login", "Account");
        }

        ViewData["SectionTitle"] = sectionTitle;
        return View("Section");
    }
}