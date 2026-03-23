using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PRN222_Group3.Models;
using PRN222_Group3.Repository;

namespace PRN222_Group3.Controllers
{
    [Authorize(Policy = "UserManageRead")]
    public class RatingController : Controller
    {
        private readonly ReviewRepository _reviewRepository = new ReviewRepository();
        private readonly ILogger<DisputeController> _logger;
        private const int PageSize = 10;

        public RatingController(ILogger<DisputeController> logger)
        {
            _logger = logger;
        }

        public async Task<IActionResult> Index(int? rating, string productTitle, string sellerUsername,
            string reviewerUsername, int pageNumber = 1)
        {
            // Store current filter values in ViewData
            if (rating.HasValue)
            {
                ViewData["CurrentRating"] = rating.Value;
            }
            if (!string.IsNullOrEmpty(productTitle))
            {
                ViewData["CurrentProduct"] = productTitle;
            }
            if (!string.IsNullOrEmpty(sellerUsername))
            {
                ViewData["CurrentSeller"] = sellerUsername;
            }
            if (!string.IsNullOrEmpty(reviewerUsername))
            {
                ViewData["CurrentReviewer"] = reviewerUsername;
            }

            var (reviews, totalPages) = await _reviewRepository.GetReviews(
                rating, productTitle, sellerUsername, pageNumber, PageSize);

            // Set up pagination info
            ViewBag.PageNumber = pageNumber;
            ViewBag.HasPreviousPage = pageNumber > 1;
            ViewBag.HasNextPage = pageNumber < totalPages;
            ViewBag.TotalPages = totalPages;

            return View(reviews);
        }

        [HttpPost]
        [Authorize(Policy = "UserManageWrite")]
        public async Task<IActionResult> Update([FromForm] Review review)
        {
            try
            {
                var existingReview = await _reviewRepository.GetByIdAsync(review.Id);
                if (existingReview == null)
                {
                    TempData["Error"] = "Review not found.";
                    return RedirectToAction(nameof(Index));
                }

                existingReview.Rating = review.Rating;
                existingReview.Comment = review.Comment;

                await _reviewRepository.Update(existingReview);
                TempData["Success"] = "Review updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating review with ID {ReviewId}", review.Id);
                TempData["Error"] = "An error occurred while updating the review.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [Authorize(Policy = "UserManageWrite")]
        public async Task<IActionResult> Delete(int reviewId)
        {
            try
            {
                await _reviewRepository.Delete(reviewId);
                TempData["Success"] = "Review deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting review with ID {ReviewId}", reviewId);
                TempData["Error"] = "An error occurred while deleting the review.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public IActionResult RedirectToPage(int pageNumber, int? rating)
        {
            if (rating.HasValue)
            {
                return RedirectToAction("Index", new { pageNumber, rating = rating.Value });
            }
            else
            {
                return RedirectToAction("Index", new { pageNumber });
            }
        }
    }
}