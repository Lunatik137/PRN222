using Microsoft.AspNetCore.Mvc;
using Project_Group3.Repository.Interfaces;

namespace Project_Group3.Controllers;

public class AdminReviewFeedbackController : Controller
{
    private readonly IReviewRepository reviewRepository;
    private readonly IFeedbackRepository feedbackRepository;

    public AdminReviewFeedbackController(IReviewRepository reviewRepository, IFeedbackRepository feedbackRepository)
    {
        this.reviewRepository = reviewRepository;
        this.feedbackRepository = feedbackRepository;
    }
    [HttpGet]
    public async Task<IActionResult> ReviewsFeedback(int? ratingFilter, string? keyword, string? sellerIdSearch, string? statusFilter, int page = 1, int pageSize = 6, string tab = "reviews")
    {
        if (HttpContext.Session.GetInt32("UserId") is null) return RedirectToAction("Login", "Account");

        int[] allowedSizes = { 6, 10, 20 };
        if (!allowedSizes.Contains(pageSize)) pageSize = 6;

        ViewBag.CurrentTab = tab;
        ViewBag.PageSize = pageSize;
        ViewBag.SellerIdSearch = sellerIdSearch;

        if (tab == "sellers")
        {
            var (sellerItems, totalSellers) = await feedbackRepository.GetPagedFeedbacksAsync(page, pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalSellers / pageSize);
            ViewBag.SellerItems = sellerItems;
        }
        else
        {
            var (reviewItems, totalReviews) = await reviewRepository.GetPagedReviewsAsync(keyword, sellerIdSearch, ratingFilter, statusFilter, page, pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalReviews / pageSize);
            ViewBag.ReviewItems = reviewItems;
            ViewBag.RatingFilter = ratingFilter;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.Keyword = keyword;
        }

        return View("~/Views/Admin/ReviewsFeedback.cshtml");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateReviewStatus(int id, string newStatus)
    {
        var allowedStatuses = new[] { "Approved", "Rejected", "Hidden", "Deleted", "Show" };
        if (!allowedStatuses.Contains(newStatus))
        {
            TempData["ErrorMessage"] = "Invalid status.";
            return RedirectToAction(nameof(ReviewsFeedback));
        }

        bool success;
        if (newStatus == "Deleted")
        {
            success = await reviewRepository.DeleteReviewAsync(id);
            if (success) 
            {
                TempData["SuccessMessage"] = "Review permanently ";
                TempData["ActionStatus"] = "Deleted";
            }
        }
        else
        {
            success = await reviewRepository.UpdateStatusAsync(id, newStatus);
            if (success) 
            {
                TempData["SuccessMessage"] = "Review marked as ";
                TempData["ActionStatus"] = newStatus;
            }
        }

        if (!success) TempData["ErrorMessage"] = "Action failed.";
        return RedirectToAction(nameof(ReviewsFeedback));
    }
}
