using Project_Group3.Models;

namespace Project_Group3.Repository.Interfaces;

public interface IReviewRepository
{
    Task<(IEnumerable<Review> Items, int Total)> GetPagedReviewsAsync(
        string? keyword,
        string? sellerIdSearch,
        int? ratingFilter,
        string? statusFilter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateStatusAsync(int reviewId, string newStatus, CancellationToken cancellationToken = default);

    Task<bool> DeleteReviewAsync(int reviewId, CancellationToken cancellationToken = default);
}
