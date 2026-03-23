using Project_Group3.Models;

namespace Project_Group3.Repository.Interfaces
{
    public interface IDisputeRepository
    {
        List<Dispute> GetAll();
        Dispute GetById(int id);
        void Update(int id, string status, string resolution);
    }
}
