using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Project_Group3.Models;
using Project_Group3.Repository.Interfaces;

namespace Project_Group3.Controllers;

public class AdminController(IUserRepository userRepository, ILogger<AdminController> logger) : Controller
{
    private const string ActionLogSessionKey = "AdminUserManagementLogs";
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "superadmin",
        "monitor"
    };

    [HttpGet]
    public IActionResult Dashboard()
    {
        if (!HasAdminAccess())
        {
            return RedirectToAction("Login", "Account");
        }

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> UserManagement([FromQuery] UserManagementFilterInput filter, CancellationToken cancellationToken)
    {
        if (!HasAdminAccess())
        {
            return RedirectToAction("Login", "Account");
        }

        var keyword = string.IsNullOrWhiteSpace(filter.Keyword) ? null : filter.Keyword.Trim();
        var page = filter.Page <= 0 ? 1 : filter.Page;
        var pageSize = filter.PageSize is < 5 or > 100 ? 10 : filter.PageSize;

        var normalizedStatus = NormalizeStatus(filter.Status);
        var (isApproved, isLocked) = MapStatus(normalizedStatus);

        var (items, total) = await userRepository.GetPagedAsync(keyword, isApproved, isLocked, page, pageSize, cancellationToken);

        if (string.Equals(normalizedStatus, "risky", StringComparison.OrdinalIgnoreCase))
        {
            items = items.Where(u => (u.RiskScore ?? 0) >= 70 || string.Equals(u.RiskLevel, "High", StringComparison.OrdinalIgnoreCase)).ToList();
            total = items.Count();
        }

        var vm = new UserManagementViewModel
        {
            Users = items.ToList(),
            Keyword = keyword,
            Status = normalizedStatus,
            Page = page,
            PageSize = pageSize,
            Total = total,
            ActionLogs = GetActionLogs()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveUser(int id, string? keyword, string? status, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        if (!HasAdminAccess())
        {
            return RedirectToAction("Login", "Account");
        }

        var success = await userRepository.ApproveAsync(id, cancellationToken);
        await TrackActionAsync(success, "Approve", id, keyword, status, page, pageSize, "Approved pending user account.");

        return RedirectToAction(nameof(UserManagement), BuildRouteValues(keyword, status, page, pageSize));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectUser(LockUserInput input, CancellationToken cancellationToken)
    {
        if (!HasAdminAccess())
        {
            return RedirectToAction("Login", "Account");
        }

        if (!ModelState.IsValid)
        {
            TempData["ActionError"] = "Reject reason is required.";
            return RedirectToAction(nameof(UserManagement), BuildRouteValues(input.Keyword, input.Status, input.Page, input.PageSize));
        }

        var success = await userRepository.RejectAsync(input.Id, input.Reason.Trim(), cancellationToken);
        await TrackActionAsync(success, "Reject", input.Id, input.Keyword, input.Status, input.Page, input.PageSize, input.Reason.Trim());

        return RedirectToAction(nameof(UserManagement), BuildRouteValues(input.Keyword, input.Status, input.Page, input.PageSize));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LockUser(LockUserInput input, CancellationToken cancellationToken)
    {
        if (!HasAdminAccess())
        {
            return RedirectToAction("Login", "Account");
        }

        if (!ModelState.IsValid)
        {
            TempData["ActionError"] = "Lock reason is required.";
            return RedirectToAction(nameof(UserManagement), BuildRouteValues(input.Keyword, input.Status, input.Page, input.PageSize));
        }

        var success = await userRepository.LockAsync(input.Id, input.Reason.Trim(), cancellationToken);
        await TrackActionAsync(success, "Lock", input.Id, input.Keyword, input.Status, input.Page, input.PageSize, input.Reason.Trim());

        return RedirectToAction(nameof(UserManagement), BuildRouteValues(input.Keyword, input.Status, input.Page, input.PageSize));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnlockUser(int id, string? keyword, string? status, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        if (!HasAdminAccess())
        {
            return RedirectToAction("Login", "Account");
        }

        var success = await userRepository.UnlockAsync(id, cancellationToken);
        await TrackActionAsync(success, "Unlock", id, keyword, status, page, pageSize, "Unlocked user account.");

        return RedirectToAction(nameof(UserManagement), BuildRouteValues(keyword, status, page, pageSize));
    }

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
        if (!HasAdminAccess())
        {
            return RedirectToAction("Login", "Account");
        }

        ViewData["SectionTitle"] = sectionTitle;
        return View("Section");
    }

    private bool HasAdminAccess()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var role = HttpContext.Session.GetString("Role");

        return userId is not null && AllowedRoles.Contains(role ?? string.Empty);
    }
    private static (bool? IsApproved, bool? IsLocked) MapStatus(string? status)
        => status?.ToLowerInvariant() switch
        {
            "pending" => (false, null),
            "approved" => (true, false),
            "locked" => (null, true),
            "active" => (true, false),
            "rejected" => (false, true),
            _ => (null, null)
        };

    private static string? NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return null;

        var normalized = status.Trim().ToLowerInvariant();
        return normalized is "pending" or "approved" or "locked" or "active" or "rejected" or "risky"
            ? normalized
            : null;
    }

    private static object BuildRouteValues(string? keyword, string? status, int page, int pageSize)
        => new
        {
            keyword,
            status = NormalizeStatus(status),
            page = page <= 0 ? 1 : page,
            pageSize = pageSize is < 5 or > 100 ? 10 : pageSize
        };

    private async Task TrackActionAsync(bool success, string actionName, int userId, string? keyword, string? status, int page, int pageSize, string details)
    {
        if (!success)
        {
            TempData["ActionError"] = $"Cannot {actionName.ToLowerInvariant()} account #{userId}.";
            return;
        }

        var target = await userRepository.GetByIdAsync(userId, CancellationToken.None);
        var targetName = target?.username ?? $"User #{userId}";
        var adminName = HttpContext.Session.GetString("Username") ?? "Admin";

        TempData["ActionSuccess"] = $"{actionName} action completed for {targetName}.";
        logger.LogInformation("Admin {AdminName} executed {ActionName} for user {UserId} ({Username}). Details: {Details}. Filters: keyword={Keyword}, status={Status}, page={Page}, pageSize={PageSize}",
            adminName, actionName, userId, targetName, details, keyword, status, page, pageSize);

        AppendActionLog(new AdminActionLogItem
        {
            AtUtc = DateTime.UtcNow,
            Action = actionName,
            Username = adminName,
            Target = targetName,
            Details = details
        });
    }

    private IReadOnlyList<AdminActionLogItem> GetActionLogs()
    {
        var json = HttpContext.Session.GetString(ActionLogSessionKey);
        if (string.IsNullOrWhiteSpace(json)) return [];

        return JsonSerializer.Deserialize<List<AdminActionLogItem>>(json) ?? [];
    }

    private void AppendActionLog(AdminActionLogItem item)
    {
        var logs = GetActionLogs().ToList();
        logs.Insert(0, item);

        if (logs.Count > 20)
        {
            logs = logs.Take(20).ToList();
        }

        HttpContext.Session.SetString(ActionLogSessionKey, JsonSerializer.Serialize(logs));
    }
}