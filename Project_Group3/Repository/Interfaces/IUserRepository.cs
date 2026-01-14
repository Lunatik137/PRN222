using Project_Group3.Models;

namespace Project_Group3.Repository.Interfaces;

public interface IUserRepository
{
    Task<List<User>> GetUsersAsync(CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task<User?> GetByCredentialsAsync(string username, string password, CancellationToken cancellationToken = default);

    Task<bool> CreateUserAsync(User user, CancellationToken cancellationToken = default);

    Task<bool> UpdateUserAsync(User user, CancellationToken cancellationToken = default);

    Task<bool> EnableTwoFactorAsync(int userId, string secret, string recoveryCodes, CancellationToken cancellationToken = default);

    Task<bool> DisableTwoFactorAsync(int userId, CancellationToken cancellationToken = default);

    Task<bool> UpdateRecoveryCodesAsync(int userId, string recoveryCodes, CancellationToken cancellationToken = default);

    Task<List<User>> GetUsersByLastLoginIpAsync(string ipAddress, CancellationToken cancellationToken = default);

    Task<(IEnumerable<User> Items, int Total)> GetPagedAsync(
        string? keyword,
        bool? isApproved,
        bool? isLocked,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<bool> ApproveAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> LockAsync(int id, string reason, CancellationToken cancellationToken = default);

    Task<bool> UnlockAsync(int id, CancellationToken cancellationToken = default);
}
