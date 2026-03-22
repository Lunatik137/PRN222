using Microsoft.EntityFrameworkCore;
using Project_Group3.Models;
using Project_Group3.Repository.Interfaces;

namespace Project_Group3.Repository;

public class FeedbackRepository(CloneEbayDbContext dbContext) : IFeedbackRepository
{
    public async Task<(IEnumerable<Feedback> Items, int Total)> GetPagedFeedbacksAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var q = dbContext.Feedbacks
            .Include(f => f.seller)
            .AsQueryable();

        var total = await q.CountAsync(cancellationToken);

        var items = await q
            .OrderBy(x => x.positiveRate)  // Order by lowest positive rate first (risky sellers)
            .ThenBy(x => x.averageRating)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
