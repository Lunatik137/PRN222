using Microsoft.EntityFrameworkCore;
using PRN222_Group3.Models;
using PRN222_Group3.Helper;

namespace PRN222_Group3.Repository
{
    public class ReviewRepository
    {
        private readonly CloneEbayDbContext _context;

        public ReviewRepository( )
        {
            _context = new CloneEbayDbContext();
        }

        public async Task<(List<Review> Reviews, int TotalPages)> GetReviews(int? rating, string? product, string? reviewer, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var query = _context.Reviews
                    .Include(r => r.Product)
                        .ThenInclude(p => p.Seller)
                    .Include(r => r.Reviewer)
                    .AsQueryable();

                // Apply rating filter if provided
                if (rating.HasValue)
                {
                    query = query.Where(r => r.Rating == rating.Value);
                }

                if (!string.IsNullOrEmpty(product))
                {
                    query = query.Where(r => r.Product.Title.Contains(product));
                }

                if (!string.IsNullOrEmpty(reviewer))
                {
                    query = query.Where(r => r.Reviewer.Username.Contains(reviewer));
                }

                // Calculate pagination
                var count = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(count / (double)pageSize);
                pageNumber = Math.Max(1, Math.Min(pageNumber, totalPages));

                var reviews = await query
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return (reviews, totalPages);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return (new List<Review>(), 0);
            }
        }

        public async Task Delete(int reviewId)
        {
            var review = await _context.Reviews.FindAsync(reviewId);
            if (review != null)
            {
                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();
            }
        }

        public async Task Update(Review review)
        {
            var existingReview = await _context.Reviews.FindAsync(review.Id);
            existingReview.CreatedAt = review.CreatedAt;
            if (existingReview != null)
            {
                existingReview.Rating = review.Rating;
                existingReview.Comment = review.Comment;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Review?> GetByIdAsync(int id) =>
            await _context.Reviews.FirstOrDefaultAsync(x => x.Id == id);

        public async Task<bool> UpdateAsync(Review review)
        {
            var existingReview = await GetByIdAsync(review.Id);
            if (existingReview is null) return false;
            existingReview.Rating = review.Rating;
            existingReview.Comment = review.Comment;
            existingReview.CreatedAt = review.CreatedAt;
            return await _context.SaveChangesAsync() > 0;
        }
    }
}