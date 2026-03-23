using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRN222_Group3.Repository;
using PRN222_Group3.Views.ViewModel;

namespace PRN222_Group3.Controllers;

[Authorize(Policy = "ReturnAndSystemNotify")]
public class ReturnRequestAdminController : Controller
{
    private readonly ReturnRequestAdminRepository _returnRequestAdminRepository;
    private readonly ILogger<ReturnRequestAdminController> _logger;

    public ReturnRequestAdminController(
        ReturnRequestAdminRepository returnRequestAdminRepository,
        ILogger<ReturnRequestAdminController> logger)
    {
        _returnRequestAdminRepository = returnRequestAdminRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? status, int page = 1, int pageSize = 10)
    {
        var result = await _returnRequestAdminRepository.GetPagedAsync(status, page, pageSize);
        var vm = new ReturnRequestAdminIndexPageViewModel
        {
            Result = result,
            StatusFilter = status
        };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var vm = await _returnRequestAdminRepository.GetDetailsAsync(id);
        if (vm == null)
            return NotFound();

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var ok = await _returnRequestAdminRepository.ApproveAsync(id);
        if (!ok)
        {
            TempData["Message"] = "Không thể duyệt: yêu cầu không tồn tại hoặc đã được xử lý.";
            TempData["MessageType"] = "error";
            return RedirectToAction(nameof(Details), new { id });
        }

        _logger.LogInformation("Admin approved return request {ReturnRequestId}", id);
        TempData["Message"] = "Đã duyệt yêu cầu hoàn trả.";
        TempData["MessageType"] = "success";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        var ok = await _returnRequestAdminRepository.RejectAsync(id);
        if (!ok)
        {
            TempData["Message"] = "Không thể từ chối: yêu cầu không tồn tại hoặc đã được xử lý.";
            TempData["MessageType"] = "error";
            return RedirectToAction(nameof(Details), new { id });
        }

        _logger.LogInformation("Admin rejected return request {ReturnRequestId}", id);
        TempData["Message"] = "Đã từ chối yêu cầu hoàn trả.";
        TempData["MessageType"] = "success";
        return RedirectToAction(nameof(Details), new { id });
    }
}
