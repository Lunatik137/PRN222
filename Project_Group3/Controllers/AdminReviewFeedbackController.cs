using Microsoft.AspNetCore.Mvc;
using Project_Group3.Repository.Interfaces;
using Project_Group3.Security;

using Microsoft.AspNetCore.SignalR;

namespace Project_Group3.Controllers;

public class AdminReviewFeedbackController : Controller
{
    private readonly IReviewRepository reviewRepository;
    private readonly IFeedbackRepository feedbackRepository;
    private readonly IUserRepository userRepository;
    private readonly IHubContext<NotificationHub> notificationHub;

    public AdminReviewFeedbackController(
        IReviewRepository reviewRepository, 
        IFeedbackRepository feedbackRepository,
        IUserRepository userRepository,
        IHubContext<NotificationHub> notificationHub)
    {
        this.reviewRepository = reviewRepository;
        this.feedbackRepository = feedbackRepository;
        this.userRepository = userRepository;
        this.notificationHub = notificationHub;
    }

    [HttpGet]
    public async Task<IActionResult> ReviewsFeedback(int? ratingFilter, string? keyword, string? sellerIdSearch, string? statusFilter, int page = 1, int pageSize = 6, string tab = "reviews")
    {
        if (!CanAccessReviewsFeedback()) return RedirectToAction("Login", "Account");

        int[] allowedSizes = { 6, 10, 20 };
        if (!allowedSizes.Contains(pageSize)) pageSize = 6;

        ViewBag.CurrentTab = tab;
        ViewBag.PageSize = pageSize;
        ViewBag.SellerIdSearch = sellerIdSearch;
        ViewBag.CanProcessReviews = CanProcessReviewsFeedback();
        ViewBag.CanDeleteReviews = CanDeleteReviewsFeedback();

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
        if (!CanProcessReviewsFeedback())
        {
            return RedirectToAction(nameof(ReviewsFeedback));
        }

        var allowedStatuses = new[] { "Approved", "Rejected", "Hidden", "Deleted", "Show" };
        if (!allowedStatuses.Contains(newStatus))
        {
            TempData["ErrorMessage"] = "Invalid status.";
            return RedirectToAction(nameof(ReviewsFeedback));
        }

        if (string.Equals(newStatus, "Deleted", StringComparison.OrdinalIgnoreCase) && !CanDeleteReviewsFeedback())
        {
            TempData["ErrorMessage"] = "Only superadmin can permanently delete reviews.";
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> WarnSeller(int sellerId)
    {
        if (!CanProcessReviewsFeedback())
        {
            return RedirectToAction(nameof(ReviewsFeedback));
        }

        var seller = await userRepository.GetByIdAsync(sellerId, HttpContext.RequestAborted);
        if (seller is null)
        {
            TempData["ErrorMessage"] = "Seller not found.";
            return RedirectToAction(nameof(ReviewsFeedback), new { tab = "sellers" });
        }

        var score = seller.RiskScore ?? 0;
        var warnMessage = $"Your seller risk level is currently at {score}. Please be aware that if your score reaches 100, your account will be automatically locked and your listings will be paused. To lower your score, please ensure all pending orders are shipped and resolve any open buyer disputes.";

        await notificationHub.Clients.All.SendAsync("ReceiveWarning", sellerId, warnMessage);

        TempData["SuccessMessage"] = $"Real-time warning sent to seller '{seller.username}'.";
        return RedirectToAction(nameof(ReviewsFeedback), new { tab = "sellers" });
    }

    private bool CanAccessReviewsFeedback()
        => HttpContext.HasAdminPermission(AdminPermissions.CanAccessReviewsFeedback);

    private bool CanProcessReviewsFeedback()
        => HttpContext.HasAdminPermission(AdminPermissions.CanProcessReviewsFeedback);

    private bool CanDeleteReviewsFeedback()
        => HttpContext.HasAdminPermission(AdminPermissions.CanDeleteReviewsFeedback);
}
