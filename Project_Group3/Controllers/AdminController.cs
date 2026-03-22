using Microsoft.AspNetCore.Mvc;
using Project_Group3.Repository.Interfaces;

namespace Project_Group3.Controllers;

public class AdminController(IReviewRepository reviewRepository, IFeedbackRepository feedbackRepository) : Controller
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
    public async Task<IActionResult> ReviewsFeedback(int? ratingFilter, string? keyword, int page = 1, string tab = "reviews")
    {
        if (HttpContext.Session.GetInt32("UserId") is null) return RedirectToAction("Login", "Account");

        int pageSize = 10;
        ViewBag.CurrentTab = tab;

        if (tab == "sellers")
        {
            var (sellerItems, totalSellers) = await feedbackRepository.GetPagedFeedbacksAsync(page, pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalSellers / pageSize);
            ViewBag.SellerItems = sellerItems;
        }
        else
        {
            var (reviewItems, totalReviews) = await reviewRepository.GetPagedReviewsAsync(keyword, ratingFilter, page, pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalReviews / pageSize);
            ViewBag.ReviewItems = reviewItems;
            ViewBag.RatingFilter = ratingFilter;
            ViewBag.Keyword = keyword;
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteReview(int id)
    {
        var success = await reviewRepository.DeleteReviewAsync(id);
        if (success) TempData["SuccessMessage"] = "Review deleted successfully.";
        else TempData["ErrorMessage"] = "Failed to delete review.";
        
        return RedirectToAction(nameof(ReviewsFeedback));
    }

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