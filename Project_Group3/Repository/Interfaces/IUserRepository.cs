using Project_Group3.Models;

namespace Project_Group3.Repository.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
