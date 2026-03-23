using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PRN222_Group3.Helper;
using PRN222_Group3.Models;

namespace PRN222_Group3.Repository
{

    public class DisputeRepository
    {
        private CloneEbayDbContext _context;

        public DisputeRepository()
        {
            _context = new CloneEbayDbContext();
        }


        public async Task<(IEnumerable<Dispute> Items, int Total)> GetDisputes(string? status, DateTime? startDate, DateTime? endDate, int page, int limit)
        {
            _context = new CloneEbayDbContext();
            var query = _context.Disputes.AsQueryable();


            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(d => d.Status.ToLower().Equals(status.ToLower()));
            }

            // Normalize date range: include entire endDate day if provided
            DateTime? adjustedStart = startDate?.Date;
            DateTime? adjustedEnd = endDate.HasValue ? endDate.Value.Date.AddDays(1).AddTicks(-1) : null;

            // Apply date filters to the query before counting so Total reflects the filtered set
            if (adjustedStart.HasValue)
            {
                query = query.Where(d => d.Order.OrderDate >= adjustedStart.Value);
            }

            if (adjustedEnd.HasValue)
            {
                query = query.Where(d => d.Order.OrderDate <= adjustedEnd.Value);
            }

            int totalRecords = await query.CountAsync();

            var items = await query
                .Include(d => d.Order)
                    .ThenInclude(o => o.Buyer)
                .Include(d => d.Order)
                    .ThenInclude(o => o.Address)
                .Include(d => d.RaisedByNavigation)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            return (items, totalRecords);
        }

        public async Task<List<ReturnRequest>> GetReturnRequestsByOrderIds(List<int?> orderIds)
        {
            _context = new CloneEbayDbContext();

            return await _context.ReturnRequests
                .Include(r => r.User)
                .Where(r => orderIds.Contains(r.OrderId))
                .ToListAsync();
        }
        public async Task<ReturnRequest?> GetReturnRequestByOrderId(int orderId)
        {
            return await _context.ReturnRequests
                .FirstOrDefaultAsync(r => r.OrderId == orderId);
        }


        public async Task<Dispute?> GetDisputeById(int id)
        {
            _context = new CloneEbayDbContext();
            return await _context.Disputes
                .Include(d => d.Order)
                    .ThenInclude(o => o.Buyer)
                .Include(d => d.Order)
                    .ThenInclude(o => o.Address)
                .Include(d => d.RaisedByNavigation)
                .FirstOrDefaultAsync(d => d.Id == id);
        }


        public async Task UpdateDispute(Dispute dispute)
        {
            _context = new CloneEbayDbContext();
            _context.Disputes.Update(dispute);
            await _context.SaveChangesAsync();
        }
    }
}

