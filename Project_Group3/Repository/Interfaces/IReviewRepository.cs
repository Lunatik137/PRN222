using Project_Group3.Models;

namespace Project_Group3.Repository.Interfaces;

public interface IReviewRepository
{
    Task<(IEnumerable<Review> Items, int Total)> GetPagedReviewsAsync(
        string? keyword,
        int? ratingFilter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteReviewAsync(int reviewId, CancellationToken cancellationToken = default);
}
