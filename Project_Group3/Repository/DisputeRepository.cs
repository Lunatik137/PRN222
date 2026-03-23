using Microsoft.EntityFrameworkCore;
using Project_Group3.Models;
using Project_Group3.Repository.Interfaces;

namespace Project_Group3.Repository
{
    public class DisputeRepository : IDisputeRepository
    {
        private readonly CloneEbayDbContext _context;

        public DisputeRepository(CloneEbayDbContext context)
        {
            _context = context;
        }

        public List<Dispute> GetAll()
        {
            return _context.Disputes
                .Include(d => d.raisedByNavigation) // user
                .Include(d => d.order)              // order
                .OrderByDescending(d => d.id)
                .ToList();
        }

        public Dispute GetById(int id)
        {
            return _context.Disputes
                .Include(d => d.raisedByNavigation)
                .Include(d => d.order)
                .FirstOrDefault(d => d.id == id);
        }

        public void Update(int id, string status, string resolution)
        {
            var d = _context.Disputes.FirstOrDefault(x => x.id == id);
            if (d != null)
            {
                d.status = status;
                d.resolution = resolution;
                _context.SaveChanges();
            }
        }
    }
}
