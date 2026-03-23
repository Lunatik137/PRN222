using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_Group3.Models;

namespace Project_Group3.Controllers;

[ApiController]
[Route("admin")]
public class AdminProductManagementController(CloneEbayDbContext dbContext) : ControllerBase
{
    private const string ProductStatusReported = "reported";
    private const string ProductStatusHidden = "hidden";
    private const string ProductStatusDeleted = "deleted";
    private const int DeleteReportThreshold = 3;
    private const int SellerAutoLockRiskThreshold = 100;

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts([FromQuery] string? status, CancellationToken cancellationToken)
    {
        var normalizedStatus = string.IsNullOrWhiteSpace(status) ? ProductStatusReported : status.Trim().ToLowerInvariant();
        var products = await dbContext.Products
            .Include(p => p.seller)
            .Include(p => p.Reviews)
            .Where(p => p.status != null && p.status.ToLower() == normalizedStatus)
            .OrderByDescending(p => p.id)
            .Select(p => new
            {
                p.id,
                p.title,
                p.description,
                p.images,
                p.status,
                sellerId = p.sellerId,
                sellerName = p.seller != null ? p.seller.username : null,
                reportCount = p.Reviews.Count(r => (r.rating ?? 0) <= 2)
            })
            .ToListAsync(cancellationToken);

        return Ok(products);
    }

    [HttpPost("products/{id:int}/report")]
    public async Task<IActionResult> ReportProduct(int id, [FromBody] ModerateProductApiRequest request, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.Include(p => p.seller).FirstOrDefaultAsync(p => p.id == id, cancellationToken);
        if (product is null)
        {
            return NotFound(new { message = $"Product #{id} not found." });
        }

        product.status = ProductStatusReported;
        ApplyRiskScore(product.seller, 10, request.Reason);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { message = $"Product #{id} moved to REPORTED." });
    }

    [HttpPost("products/{id:int}/hide")]
    public async Task<IActionResult> HideProduct(int id, [FromBody] ModerateProductApiRequest request, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.Include(p => p.seller).FirstOrDefaultAsync(p => p.id == id, cancellationToken);
        if (product is null)
        {
            return NotFound(new { message = $"Product #{id} not found." });
        }

        if (!string.Equals(product.status, ProductStatusReported, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Only REPORTED products can be hidden." });
        }

        product.status = ProductStatusHidden;
        ApplyRiskScore(product.seller, 15, request.Reason);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { message = $"Product #{id} moved to HIDDEN." });
    }

    [HttpPost("products/{id:int}/delete")]
    public async Task<IActionResult> DeleteProduct(int id, [FromBody] ModerateProductApiRequest request, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .Include(p => p.seller)
            .Include(p => p.Reviews)
            .FirstOrDefaultAsync(p => p.id == id, cancellationToken);
        if (product is null)
        {
            return NotFound(new { message = $"Product #{id} not found." });
        }

        if (!string.Equals(product.status, ProductStatusReported, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Only REPORTED products can be deleted." });
        }

        var reportCount = product.Reviews.Count(r => (r.rating ?? 0) <= 2);
        product.status = ProductStatusDeleted;
        ApplyRiskScore(product.seller, reportCount >= DeleteReportThreshold ? 30 : 20, request.Reason);

        if (request.LockSeller && product.seller is not null && !product.seller.isLocked)
        {
            product.seller.isLocked = true;
            product.seller.lockedAt = DateTime.UtcNow;
            product.seller.lockedReason = $"Locked by admin after product delete: {request.Reason}";
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { message = $"Product #{id} moved to DELETED.", reportCount });
    }

    [HttpPost("users/{sellerId:int}/lock")]
    public async Task<IActionResult> LockSeller(int sellerId, [FromBody] LockSellerApiRequest request, CancellationToken cancellationToken)
    {
        var seller = await dbContext.Users.FirstOrDefaultAsync(u => u.id == sellerId, cancellationToken);
        if (seller is null)
        {
            return NotFound(new { message = $"Seller #{sellerId} not found." });
        }

        seller.isLocked = true;
        seller.lockedAt = DateTime.UtcNow;
        seller.lockedReason = request.Reason.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { message = $"Seller #{sellerId} locked." });
    }

    private static void ApplyRiskScore(User? seller, int delta, string reason)
    {
        if (seller is null)
        {
            return;
        }

        seller.RiskScore = (seller.RiskScore ?? 0) + delta;
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
            seller.isLocked = true;
            seller.lockedAt = DateTime.UtcNow;
            seller.lockedReason = $"{reason}. Auto-locked by risk threshold {SellerAutoLockRiskThreshold}.";
        }
    }
}

public sealed class ModerateProductApiRequest
{
    public string Reason { get; init; } = "Moderated by admin";
    public bool LockSeller { get; init; }
}

public sealed class LockSellerApiRequest
{
    public string Reason { get; init; } = "Locked by admin";
}
