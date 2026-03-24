using Project_Group3.Models;

namespace Project_Group3.Repository.Interfaces;

public interface IFeedbackRepository
{
    Task<(IEnumerable<Feedback> Items, int Total)> GetPagedFeedbacksAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
