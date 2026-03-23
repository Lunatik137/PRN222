using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PRN222_Group3.Repository;

namespace PRN222_Group3.Controllers
{
    [Authorize(Policy = "UserManageRead")]
    public class FeedbackController : Controller
    {

        private readonly FeedbackRepository _feedbackRepository = new FeedbackRepository();
        private const int PageSize = 10;


        public async Task<ActionResult> Index(string? sellerName,
        int? rating,
        int pageNumber = 1)
        {
            ViewData["CurrentSellerName"] = sellerName;
            ViewData["CurrentRating"] = rating;
            ViewData["CurrentPageNumber"] = pageNumber;

            var (items, total) = await _feedbackRepository.SearchFeedbacksAsync(sellerName, rating, pageNumber, PageSize);

            ViewBag.Total = total;
            ViewBag.PageNumber = pageNumber;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / PageSize);
            ViewBag.PageSize = PageSize;
            ViewBag.HasPreviousPage = pageNumber > 1;
            ViewBag.HasNextPage = (pageNumber * PageSize) < total;
            return View(items);
        }
    }
}
