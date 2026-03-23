using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRN222_Group3.Repository;

[Authorize(Policy = "UserManageRead")]
public class UserDashboardController : Controller
{
    private UserRepository CreateRepo() => new UserRepository();

    [HttpGet]
    public async Task<IActionResult> UserDashboard(string? keyword, bool? isApproved, bool? isLocked, int page = 1, int pageSize = 10)
    {
        var repo = CreateRepo();
        var (items, total) = await repo.GetPagedAsync(keyword, isApproved, isLocked, page, pageSize);
        ViewBag.Total = total;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.Keyword = keyword;
        ViewBag.IsApproved = isApproved;
        ViewBag.IsLocked = isLocked;
        
        // Get current user's 2FA status
        var username = User.Identity?.Name;
        if (!string.IsNullOrEmpty(username))
        {
            var currentUser = await repo.GetUserByUsername(username);
            ViewBag.TwoFactorEnabled = currentUser?.IsTwoFactorEnabled ?? false;
        }
        else
        {
            ViewBag.TwoFactorEnabled = false;
        }
        
        return View(items);
    }

    [HttpPost]
    [Authorize(Policy = "UserManageWrite")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, string? returnUrl = null)
    {
        var ok = await CreateRepo().ApproveAsync(id);
        TempData["Message"] = ok ? "Approved" : "Approve failed";
        return Redirect(returnUrl ?? Url.Action("UserDashboard")!);
    }

    [HttpPost]
    [Authorize(Policy = "UserManageWrite")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Lock(int id, string reason, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Message"] = "Lock reason is required";
            TempData["MessageType"] = "error";
            return Redirect(returnUrl ?? Url.Action("UserDashboard")!);
        }

        var ok = await CreateRepo().LockAsync(id, reason.Trim());
        TempData["Message"] = ok ? "Locked" : "Lock failed";
        return Redirect(returnUrl ?? Url.Action("UserDashboard")!);
    }

    [HttpPost]
    [Authorize(Policy = "UserManageWrite")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(int id, string? returnUrl = null)
    {
        var ok = await CreateRepo().UnlockAsync(id);
        TempData["Message"] = ok ? "Unlocked" : "Unlock failed";
        return Redirect(returnUrl ?? Url.Action("UserDashboard")!);
    }
}
