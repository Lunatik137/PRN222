using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_Group3.Models;
using Project_Group3.Security;
using Project_Group3.ViewModel;

namespace Project_Group3.Controllers;

public class ReturnRequestAdminController(CloneEbayDbContext dbContext) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? status, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        if (!HasAdminAccess())
        {
            return RedirectToAction("Login", "Account");
        }

        page = page <= 0 ? 1 : page;
        pageSize = pageSize is < 5 or > 100 ? 10 : pageSize;

        var query = dbContext.ReturnRequests
            .AsNoTracking()
            .Include(x => x.user)
            .Include(x => x.order)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.status != null && x.status.ToLower() == status.Trim().ToLower());
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.createdAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ReturnRequestAdminListItemViewModel
            {
                ReturnRequestId = x.id,
                OrderId = x.orderId,
                BuyerUsername = x.user != null ? x.user.username : null,
                BuyerEmail = x.user != null ? x.user.email : null,
                OrderStatus = x.order != null ? x.order.status : null,
                ReturnStatus = x.status,
                CreatedAt = x.createdAt,
                ReasonPreview = x.reason != null && x.reason.Length > 100 ? x.reason.Substring(0, 100) + "..." : x.reason
            })
            .ToListAsync(cancellationToken);

        var vm = new ReturnRequestAdminIndexPageViewModel
        {
            Items = items,
            Status = status,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
    {
        if (!HasAdminAccess())
        {
            return RedirectToAction("Login", "Account");
        }

        var request = await dbContext.ReturnRequests
            .AsNoTracking()
            .Include(x => x.user)
            .Include(x => x.order)
            .FirstOrDefaultAsync(x => x.id == id, cancellationToken);

        if (request is null)
        {
            return NotFound();
        }

        var vm = new ReturnRequestAdminDetailsViewModel
        {
            ReturnRequestId = request.id,
            OrderId = request.orderId,
            BuyerUsername = request.user?.username,
            BuyerEmail = request.user?.email,
            OrderStatus = request.order?.status,
            OrderTotal = request.order?.totalPrice,
            OrderDate = request.order?.orderDate,
            ReturnStatus = request.status,
            Reason = request.reason,
            CreatedAt = request.createdAt,
            CanProcess = string.Equals(request.status, "pending", StringComparison.OrdinalIgnoreCase),
            Images = string.IsNullOrWhiteSpace(request.Images)
                ? []
                : request.Images.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, CancellationToken cancellationToken = default)
    {
        if (!HasAdminAccess())
        {
            return RedirectToAction("Login", "Account");
        }

        var request = await dbContext.ReturnRequests
            .Include(x => x.order)
            .FirstOrDefaultAsync(x => x.id == id, cancellationToken);

        if (request is null || !string.Equals(request.status, "pending", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Message"] = "Không thể duyệt: yêu cầu không tồn tại hoặc đã được xử lý.";
            TempData["MessageType"] = "error";
            return RedirectToAction(nameof(Details), new { id });
        }

        request.status = "Completed";
        if (request.order is not null)
        {
            request.order.status = "Returned";
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["Message"] = "Đã duyệt yêu cầu hoàn trả.";
        TempData["MessageType"] = "success";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, CancellationToken cancellationToken = default)
    {
        if (!HasAdminAccess())
        {
            return RedirectToAction("Login", "Account");
        }

        var request = await dbContext.ReturnRequests
            .Include(x => x.order)
            .FirstOrDefaultAsync(x => x.id == id, cancellationToken);

        if (request is null || !string.Equals(request.status, "pending", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Message"] = "Không thể từ chối: yêu cầu không tồn tại hoặc đã được xử lý.";
            TempData["MessageType"] = "error";
            return RedirectToAction(nameof(Details), new { id });
        }

        request.status = "Rejected";
        if (request.order is not null)
        {
            request.order.status = "Completed";
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["Message"] = "Đã từ chối yêu cầu hoàn trả.";
        TempData["MessageType"] = "success";
        return RedirectToAction(nameof(Details), new { id });
    }

    private bool HasAdminAccess()
        => HttpContext.HasAdminPermission(AdminPermissions.CanAccessRefunds);
}
