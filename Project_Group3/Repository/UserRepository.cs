using Microsoft.EntityFrameworkCore;
using Project_Group3.Models;
using Project_Group3.Repository.Interfaces;

namespace Project_Group3.Repository;

public sealed class UserRepository(CloneEbayDbContext dbContext) : IUserRepository
{
    public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await dbContext.Users.FirstOrDefaultAsync(x => x.id == id, cancellationToken);
}
