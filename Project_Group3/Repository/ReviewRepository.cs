using Microsoft.EntityFrameworkCore;
using Project_Group3.Models;
using Project_Group3.Repository.Interfaces;

namespace Project_Group3.Repository;

public class ReviewRepository(CloneEbayDbContext dbContext) : IReviewRepository
{
    public async Task<(IEnumerable<Review> Items, int Total)> GetPagedReviewsAsync(
        string? keyword,
        string? sellerIdSearch,
        int? ratingFilter,
        string? statusFilter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var q = dbContext.Reviews
            .Include(r => r.product)
            .Include(r => r.reviewer)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            if (int.TryParse(kw, out int idKeyword))
            {
                q = q.Where(x => (x.product != null && x.product.title != null && x.product.title.Contains(kw)) || (x.product != null && x.product.id == idKeyword));
            }
            else
            {
                q = q.Where(x => x.product != null && x.product.title != null && x.product.title.Contains(kw));
            }
        }

        if (!string.IsNullOrWhiteSpace(sellerIdSearch))
        {
            if (int.TryParse(sellerIdSearch.Trim(), out int sId))
            {
                q = q.Where(x => x.product != null && x.product.sellerId == sId);
            }
        }

        if (ratingFilter.HasValue && ratingFilter.Value > 0)
        {
            q = q.Where(x => x.rating == ratingFilter.Value);
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            q = q.Where(x => x.status == statusFilter);
        }

        var total = await q.CountAsync(cancellationToken);

        var items = await q
            .OrderByDescending(x => x.createdAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<bool> UpdateStatusAsync(int reviewId, string newStatus, CancellationToken cancellationToken = default)
    {
        var review = await dbContext.Reviews.FirstOrDefaultAsync(r => r.id == reviewId, cancellationToken);
        if (review is null) return false;

        review.status = newStatus;
        return await dbContext.SaveChangesAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeleteReviewAsync(int reviewId, CancellationToken cancellationToken = default)
    {
        var review = await dbContext.Reviews.FirstOrDefaultAsync(r => r.id == reviewId, cancellationToken);
        if (review is null) return false;

        dbContext.Reviews.Remove(review);
        return await dbContext.SaveChangesAsync(cancellationToken) > 0;
    }
}
