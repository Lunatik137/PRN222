using PRN222_Group3.Models;

namespace PRN222_Group3.Service
{
    public interface IUserService
    {
        Task<User?> AuthenticateAsync(string username, string password);
        Task<(IEnumerable<User> Items, int Total)> GetPagedAsync(string? keyword, bool? isApproved, bool? isLocked, int page, int pageSize);
        Task<User?> GetByIdAsync(int id);
        Task<bool> ApproveAsync(int id);
        Task<bool> LockAsync(int id, string reason);
        Task<bool> UnlockAsync(int id);
        Task<bool> CreateUserAsync(User user);


    }
}
