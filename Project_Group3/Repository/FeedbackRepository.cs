using Microsoft.EntityFrameworkCore;
using PRN222_Group3.Models;

namespace PRN222_Group3.Repository
{
    public class FeedbackRepository
    {
        private readonly CloneEbayDbContext _context;
        public FeedbackRepository()
        {
            _context = new CloneEbayDbContext();
        }

        public async Task<Feedback?> GetByIdAsync(int id)
        {
            return await _context.Feedbacks.FindAsync(id);
        }

        public async Task UpdateAsync(Feedback feedback)
        {
            var feedbackToUpdate = await GetByIdAsync(feedback.Id);
            if (feedbackToUpdate != null)
            {
                feedbackToUpdate.PositiveRate = feedback.PositiveRate;
                feedbackToUpdate.AverageRating = feedback.AverageRating;
            }
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var feedback = await GetByIdAsync(id);
            if (feedback != null)
            {
                _context.Feedbacks.Remove(feedback);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<(IEnumerable<Feedback> Items, int Total)> SearchFeedbacksAsync(string? sellerName, int? rating, int pageNumber = 1, int pageSize = 10)
        {
            var query = _context.Feedbacks
                                .Include(f => f.Seller)
                                .AsQueryable();

            if (!string.IsNullOrEmpty(sellerName))
            {
                query = query.Where(f => f.Seller.Username.Contains(sellerName));
            }

            if (rating.HasValue)
                if (rating.HasValue)
                {
                    query = query.Where(f => f.AverageRating >= rating.Value && f.AverageRating < (rating.Value + 1));
                }

            //query = query.OrderByDescending(f => f.);

            int total = await query.CountAsync();
            var items = await query
                                .Skip((pageNumber - 1) * pageSize)
                                .Take(pageSize)
                                .ToListAsync();


            return (items, total);
        }
    }
}
