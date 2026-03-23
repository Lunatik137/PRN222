using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_Group3.Models;
using Project_Group3.Repository.Interfaces;

namespace Project_Group3.Controllers;

public class AdminController(
    IUserRepository userRepository,
    CloneEbayDbContext dbContext,
    ILogger<AdminController> logger) : Controller
{
    private const string ProductStatusActive = "active";
    private const string ProductStatusReported = "reported";
    private const string ProductStatusHidden = "hidden";
    private const string ProductStatusDeleted = "deleted";
    private const int DeleteReportThreshold = 3;
    private const int RiskScorePerReport = 10;
    private const int RiskScorePerHidden = 15;
    private const int RiskScorePerDeleted = 30;
    private const int SellerAutoLockRiskThreshold = 100;
    private const string ActionLogSessionKey = "AdminUserManagementLogs";
    private const string ProductModerationLogSessionKey = "AdminProductModerationLogs";
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "superadmin",
        "monitor"
    };

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
    public async Task<IActionResult> ProductModeration([FromQuery] ProductModerationFilterInput filter, CancellationToken cancellationToken)
    {
        if (!HasSuperAdminAccess())
        {
            return RedirectToAction("Login", "Account");
        }

        var normalizedStatus = NormalizeProductStatus(filter.Status);
        var keyword = string.IsNullOrWhiteSpace(filter.Keyword) ? null : filter.Keyword.Trim();
        var productsQuery = dbContext.Products
            .Include(p => p.seller)
            .AsQueryable();

        productsQuery = normalizedStatus switch
        {
            ProductStatusActive => productsQuery.Where(p => p.status != null && p.status.ToLower() == ProductStatusActive),
            ProductStatusReported => productsQuery.Where(p => p.status != null && p.status.ToLower() == ProductStatusReported),
            ProductStatusHidden => productsQuery.Where(p => p.status != null && p.status.ToLower() == ProductStatusHidden),
            ProductStatusDeleted => productsQuery.Where(p => p.status != null && p.status.ToLower() == ProductStatusDeleted),
            _ => productsQuery.Where(p => p.status != null && p.status.ToLower() == ProductStatusReported)
        };

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            productsQuery = productsQuery.Where(p =>
                (p.title ?? string.Empty).Contains(keyword) ||
                (p.description ?? string.Empty).Contains(keyword) ||
                ((p.seller != null ? p.seller.username : string.Empty) ?? string.Empty).Contains(keyword));
        }

        var products = await productsQuery
           .Include(p => p.Reviews)
           .OrderByDescending(p => p.id)
           .Take(100)
           .ToListAsync(cancellationToken);

        var moderationItems = products
            .Select(product => new ProductModerationItemViewModel
            {
                Product = product,
                ReportCount = GetReportCount(product)
            })
            .ToList();

        var vm = new ProductModerationViewModel
        {
            Products = moderationItems,
            ActionLogs = GetProductModerationLogs(),
            Keyword = keyword,
            Status = normalizedStatus,
            Total = moderationItems.Count
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReportProduct(ReportProductInput input, CancellationToken cancellationToken)
    {
        if (!HasAdminAccess())
        {
            return RedirectToAction("Login", "Account");
        }

        if (!ModelState.IsValid)
        {
            TempData["ActionError"] = "Reason is required to report a product.";
            return RedirectToAction(nameof(ProductModeration), BuildProductRouteValues(input.Status, input.Keyword));
        }

        var product = await dbContext.Products
            .Include(p => p.seller)
            .FirstOrDefaultAsync(p => p.id == input.ProductId, cancellationToken);
        if (product is null)
        {
            TempData["ActionError"] = $"Product #{input.ProductId} not found.";
            return RedirectToAction(nameof(ProductModeration), BuildProductRouteValues(input.Status, input.Keyword));
        }

        product.status = ProductStatusReported;
        var isLocked = ApplySellerRiskPolicy(product.seller, RiskScorePerReport, $"Product report: {input.Reason.Trim()}");

        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["ActionSuccess"] = isLocked
            ? $"Product #{product.id} was marked as reported. Seller was auto-locked by risk policy."
            : $"Product #{product.id} was marked as reported.";

        AppendProductModerationLog(new AdminActionLogItem
        {
            AtUtc = DateTime.UtcNow,
            Action = "Report Product",
            Username = HttpContext.Session.GetString("Username") ?? "Admin",
            Target = product.title ?? $"Product #{product.id}",
            Details = input.Reason.Trim()
        });

        return RedirectToAction(nameof(ProductModeration), BuildProductRouteValues(input.Status, input.Keyword));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HideProduct(ModerateProductInput input, CancellationToken cancellationToken)
    {
        if (!HasSuperAdminAccess())
        {
            return RedirectToAction("Login", "Account");
        }

        if (!ModelState.IsValid)
        {
            TempData["ActionError"] = "Reason is required for product moderation actions.";
            return RedirectToAction(nameof(ProductModeration), BuildProductRouteValues(input.Status, input.Keyword));
        }

        var product = await dbContext.Products
            .Include(p => p.seller)
            .FirstOrDefaultAsync(p => p.id == input.ProductId, cancellationToken);
        if (product is null)
        {
            TempData["ActionError"] = $"Product #{input.ProductId} not found.";
            return RedirectToAction(nameof(ProductModeration), BuildProductRouteValues(input.Status, input.Keyword));
        }

        if (!CanModerateProduct(product))
        {
            TempData["ActionError"] = $"Product #{product.id} must be in reported status before hide/delete.";
            return RedirectToAction(nameof(ProductModeration), BuildProductRouteValues(input.Status, input.Keyword));
        }

        product.status = ProductStatusHidden;

        var isAutoLocked = ApplySellerRiskPolicy(product.seller, RiskScorePerHidden, $"Hidden product #{product.id}: {input.Reason.Trim()}");
        if (input.LockSeller && product.seller is not null && !product.seller.isLocked)
        {
            LockSeller(product.seller, $"Locked due to product moderation: {input.Reason.Trim()}");
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        TempData["ActionSuccess"] = isAutoLocked
                    ? $"Product #{product.id} has been hidden. Seller was auto-locked by risk policy."
                    : $"Product #{product.id} has been hidden."; AppendProductModerationLog(new AdminActionLogItem
        {
            AtUtc = DateTime.UtcNow,
            Action = "Hide Product",
            Username = HttpContext.Session.GetString("Username") ?? "SuperAdmin",
            Target = product.title ?? $"Product #{product.id}",
            Details = input.Reason.Trim()
        });

        return RedirectToAction(nameof(ProductModeration), BuildProductRouteValues(input.Status, input.Keyword));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProduct(ModerateProductInput input, CancellationToken cancellationToken)
    {
        if (!HasSuperAdminAccess())
        {
            return RedirectToAction("Login", "Account");
        }

        if (!ModelState.IsValid)
        {
            TempData["ActionError"] = "Reason is required for product moderation actions.";
            return RedirectToAction(nameof(ProductModeration), BuildProductRouteValues(input.Status, input.Keyword));
        }

        var product = await dbContext.Products
            .Include(p => p.seller)
            .FirstOrDefaultAsync(p => p.id == input.ProductId, cancellationToken);
        if (product is null)
        {
            TempData["ActionError"] = $"Product #{input.ProductId} not found.";
            return RedirectToAction(nameof(ProductModeration), BuildProductRouteValues(input.Status, input.Keyword));
        }

        if (!CanModerateProduct(product))
        {
            TempData["ActionError"] = $"Product #{product.id} must be in reported status before hide/delete.";
            return RedirectToAction(nameof(ProductModeration), BuildProductRouteValues(input.Status, input.Keyword));
        }

        product.status = ProductStatusDeleted;
        var isAutoLocked = ApplySellerRiskPolicy(product.seller, RiskScorePerDeleted, $"Deleted product #{product.id}: {input.Reason.Trim()}");

        if (input.LockSeller && product.seller is not null && !product.seller.isLocked)
        {
            LockSeller(product.seller, $"Locked due to severe product violation: {input.Reason.Trim()}");
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        TempData["ActionSuccess"] = isAutoLocked
                   ? $"Product #{product.id} has been deleted. Seller was auto-locked by risk policy."
                   : $"Product #{product.id} has been deleted."; AppendProductModerationLog(new AdminActionLogItem
        {
            AtUtc = DateTime.UtcNow,
            Action = "Delete Product",
            Username = HttpContext.Session.GetString("Username") ?? "SuperAdmin",
            Target = product.title ?? $"Product #{product.id}",
            Details = input.Reason.Trim()
        });

        return RedirectToAction(nameof(ProductModeration), BuildProductRouteValues(input.Status, input.Keyword));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AutoModerateProduct(ModerateProductInput input, CancellationToken cancellationToken)
    {
        if (!HasSuperAdminAccess())
        {
            return RedirectToAction("Login", "Account");
        }

        if (!ModelState.IsValid)
        {
            TempData["ActionError"] = "Reason is required for product moderation actions.";
            return RedirectToAction(nameof(ProductModeration), BuildProductRouteValues(input.Status, input.Keyword));
        }

        var product = await dbContext.Products
             .Include(p => p.Reviews)
             .Include(p => p.seller)
             .FirstOrDefaultAsync(p => p.id == input.ProductId, cancellationToken);
        if (product is null)
        {
            TempData["ActionError"] = $"Product #{input.ProductId} not found.";
            return RedirectToAction(nameof(ProductModeration), BuildProductRouteValues(input.Status, input.Keyword));
        }

        var reportCount = GetReportCount(product);
        if (reportCount >= DeleteReportThreshold)
        {
            product.status = ProductStatusDeleted;
            var isAutoLocked = ApplySellerRiskPolicy(product.seller, RiskScorePerDeleted, $"Auto delete product #{product.id}: {input.Reason.Trim()}");
            TempData["ActionSuccess"] = $"Product #{product.id} has been deleted automatically (report count = {reportCount}).";
            if (isAutoLocked)
            {
                TempData["ActionSuccess"] += " Seller was auto-locked by risk policy.";
            }
        }
        else
        {
            product.status = ProductStatusHidden;
            var isAutoLocked = ApplySellerRiskPolicy(product.seller, RiskScorePerHidden, $"Auto hide product #{product.id}: {input.Reason.Trim()}");
            TempData["ActionSuccess"] = $"Product #{product.id} has been hidden automatically (report count = {reportCount}).";
            if (isAutoLocked)
            {
                TempData["ActionSuccess"] += " Seller was auto-locked by risk policy.";
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        AppendProductModerationLog(new AdminActionLogItem
        {
            AtUtc = DateTime.UtcNow,
            Action = "Auto Moderate Product",
            Username = HttpContext.Session.GetString("Username") ?? "SuperAdmin",
            Target = product.title ?? $"Product #{product.id}",
            Details = $"{input.Reason.Trim()} (report count = {reportCount})"
        });

        return RedirectToAction(nameof(ProductModeration), BuildProductRouteValues(input.Status, input.Keyword));
    }

    [HttpGet]
    public IActionResult OrderManagement() => AdminSection("Order Management");

    [HttpGet]
    public IActionResult ReviewsFeedback() => AdminSection("Reviews & Feedback");

    [HttpGet]
    public IActionResult ComplaintsDisputes() => AdminSection("Complaints / Disputes");

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
    private bool HasSuperAdminAccess()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var role = HttpContext.Session.GetString("Role");

        return userId is not null && string.Equals(role, "superadmin", StringComparison.OrdinalIgnoreCase);
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
    private static string NormalizeProductStatus(string? status)
    {
        var normalized = status?.Trim().ToLowerInvariant();
        return normalized is ProductStatusActive or ProductStatusReported or ProductStatusHidden or ProductStatusDeleted
            ? normalized
            : ProductStatusReported;
    }

    private static object BuildProductRouteValues(string? status, string? keyword)
        => new
        {
            status = NormalizeProductStatus(status),
            keyword = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim()
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

    private IReadOnlyList<AdminActionLogItem> GetProductModerationLogs()
    {
        var json = HttpContext.Session.GetString(ProductModerationLogSessionKey);
        if (string.IsNullOrWhiteSpace(json)) return [];

        return JsonSerializer.Deserialize<List<AdminActionLogItem>>(json) ?? [];
    }

    private void AppendProductModerationLog(AdminActionLogItem item)
    {
        var logs = GetProductModerationLogs().ToList();
        logs.Insert(0, item);

        if (logs.Count > 20)
        {
            logs = logs.Take(20).ToList();
        }

        HttpContext.Session.SetString(ProductModerationLogSessionKey, JsonSerializer.Serialize(logs));
    }

    private static int GetReportCount(Product product)
        => product.Reviews.Count(r => (r.rating ?? 0) <= 2);

    private static bool CanModerateProduct(Product product)
        => string.Equals(product.status, ProductStatusReported, StringComparison.OrdinalIgnoreCase);

    private static void LockSeller(User seller, string reason)
    {
        seller.isLocked = true;
        seller.lockedAt = DateTime.UtcNow;
        seller.lockedReason = reason;
    }

    private static bool ApplySellerRiskPolicy(User? seller, int riskDelta, string reason)
    {
        if (seller is null)
        {
            return false;
        }

        seller.RiskScore = (seller.RiskScore ?? 0) + riskDelta;
        seller.LastRiskAssessment = DateTime.UtcNow;

        seller.RiskLevel = seller.RiskScore switch
        {
            >= 100 => "Critical",
            >= 70 => "High",
            >= 40 => "Medium",
            _ => "Low"
        };

        if (seller.RiskScore > SellerAutoLockRiskThreshold && !seller.isLocked)
        {
            LockSeller(seller, $"{reason}. Auto-locked by risk threshold {SellerAutoLockRiskThreshold}.");
            return true;
        }

        return false;
    }
}
