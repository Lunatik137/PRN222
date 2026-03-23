using Microsoft.EntityFrameworkCore;
using PRN222_Group3.Helper;
using PRN222_Group3.Models;
using PRN222_Group3.Views.ViewModel;

namespace PRN222_Group3.Repository;

public class ReturnRequestAdminRepository
{
    private readonly CloneEbayDbContext _context;

    public ReturnRequestAdminRepository(CloneEbayDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResult<ReturnRequestAdminListItemViewModel>> GetPagedAsync(string? statusFilter, int page, int pageSize)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : Math.Min(pageSize, 100);

        var query = _context.ReturnRequests
            .AsNoTracking()
            .Include(rr => rr.User)
            .Include(rr => rr.Order)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(statusFilter))
            query = query.Where(rr => rr.Status == statusFilter);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(rr => rr.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(rr => new ReturnRequestAdminListItemViewModel
            {
                ReturnRequestId = rr.Id,
                OrderId = rr.OrderId,
                BuyerUsername = rr.User != null ? rr.User.Username : null,
                OrderStatus = rr.Order != null ? rr.Order.Status : null,
                ReturnStatus = rr.Status,
                CreatedAt = rr.CreatedAt,
                ReasonPreview = rr.Reason != null && rr.Reason.Length > 80 ? rr.Reason.Substring(0, 80) + "…" : rr.Reason
            })
            .ToListAsync();

        return new PaginatedResult<ReturnRequestAdminListItemViewModel>
        {
            Items = items,
            TotalCount = total,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    public async Task<ReturnRequestAdminDetailsViewModel?> GetDetailsAsync(int returnRequestId)
    {
        var rr = await _context.ReturnRequests
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Order)
            .FirstOrDefaultAsync(x => x.Id == returnRequestId);

        if (rr == null)
            return null;

        List<string> images;
        if (string.IsNullOrWhiteSpace(rr.Images))
            images = new List<string>();
        else
            images = rr.Images.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        return new ReturnRequestAdminDetailsViewModel
        {
            ReturnRequestId = rr.Id,
            OrderId = rr.OrderId,
            BuyerId = rr.UserId,
            BuyerUsername = rr.User?.Username,
            BuyerEmail = rr.User?.Email,
            OrderStatus = rr.Order?.Status,
            OrderTotal = rr.Order?.TotalPrice,
            OrderDate = rr.Order?.OrderDate,
            ReturnStatus = rr.Status,
            Reason = rr.Reason,
            CreatedAt = rr.CreatedAt,
            ImageFileNames = images,
            CanProcess = string.Equals(rr.Status, "Pending", StringComparison.OrdinalIgnoreCase)
        };
    }

    /// <summary>Chỉ khi Status = Pending: ReturnRequest → Completed, Order → Returned.</summary>
    public async Task<bool> ApproveAsync(int returnRequestId)
    {
        var rr = await _context.ReturnRequests
            .Include(r => r.Order)
            .FirstOrDefaultAsync(r => r.Id == returnRequestId);

        if (rr == null || !string.Equals(rr.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            return false;

        rr.Status = "Completed";
        if (rr.Order != null)
            rr.Order.Status = "Returned";

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>Chỉ khi Status = Pending: ReturnRequest → Rejected, Order → Completed.</summary>
    public async Task<bool> RejectAsync(int returnRequestId)
    {
        var rr = await _context.ReturnRequests
            .Include(r => r.Order)
            .FirstOrDefaultAsync(r => r.Id == returnRequestId);

        if (rr == null || !string.Equals(rr.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            return false;

        rr.Status = "Rejected";
        if (rr.Order != null)
            rr.Order.Status = "Completed";

        await _context.SaveChangesAsync();
        return true;
    }
}
