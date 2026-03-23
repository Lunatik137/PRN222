using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRN222_Group3.Models;
using PRN222_Group3.Repository;
// SignalR removed

namespace PRN222_Group3.Controllers
{
    public class DisputeController : Controller
    {

        private readonly DisputeRepository _disputeRepository = new DisputeRepository();
        private readonly ILogger<DisputeController> _logger;

        public DisputeController(ILogger<DisputeController> logger)
        {
            _logger = logger;
        }

        public async Task<IActionResult> Index(
            string status,
            string startDate,
            string endDate,
            int pageNumber = 1)
        {
            ViewData["CurrentStatus"] = status;
            ViewData["CurrentStartDate"] = startDate;
            ViewData["CurrentEndDate"] = endDate;

            Console.WriteLine(status);
            Console.WriteLine(startDate);
            Console.WriteLine(endDate);

            // string to datetime
            DateTime? startDateTime = !string.IsNullOrEmpty(startDate) ? DateTime.Parse(startDate) : null;
            DateTime? endDateTime = !string.IsNullOrEmpty(endDate) ? DateTime.Parse(endDate) : null;

            // go to repository to get data
            //var (item, total) = await _disputeRepository.GetDisputes(status, startDateTime, endDateTime, pageNumber, 10);
            var (disputes, total) = await _disputeRepository.GetDisputes(
        status, startDateTime, endDateTime, pageNumber, 10);
            var orderIds = disputes.Select(d => d.OrderId).ToList();
            var returnRequests = await _disputeRepository.GetReturnRequestsByOrderIds(orderIds);
            ViewBag.ReturnRequests = returnRequests;
            ViewBag.Total = total;
            ViewBag.PageNumber = pageNumber;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / 10);
            ViewBag.PageSize = 10;
            ViewBag.HasPreviousPage = pageNumber > 1;
            ViewBag.HasNextPage = (pageNumber * 10) < total;

            Console.WriteLine(total);

            return View(disputes);
        }

        public async Task<IActionResult> GetById(int id)
        {
            var dispute = await _disputeRepository.GetDisputeById(id);
            var returnRequest = await _disputeRepository.GetReturnRequestByOrderId(id);
            if (dispute == null)
            {
                return NotFound();
            }

            return Json(new
            {
                id = dispute.Id,
                orderId = dispute.OrderId,
                raisedBy = dispute.RaisedBy,
                description = dispute.Description,
                status = dispute.Status,
                resolution = dispute.Resolution,
                images = returnRequest?.Images
            });
        }

        [HttpPost]
        public async Task<IActionResult> Update(Dispute dispute)
        {
            try
            {
                _logger?.LogInformation("Updating dispute id={DisputeId}", dispute?.Id);
                await _disputeRepository.UpdateDispute(dispute);
                _logger?.LogInformation("Dispute id={DisputeId} updated in repository", dispute?.Id);

                // No SignalR broadcasting configured; simply return OK after updating
                _logger?.LogInformation("No SignalR broadcast (removed). Returning OK for dispute id={DisputeId}", dispute?.Id);
                return Ok(); // Return a success response
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}