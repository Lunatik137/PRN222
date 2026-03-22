using Microsoft.EntityFrameworkCore;
using Project_Group3.Models;
using Project_Group3.Repository.Interfaces;

namespace Project_Group3.Repository;

public sealed class UserRepository(CloneEbayDbContext dbContext) : IUserRepository
{
    public Task<List<User>> GetUsersAsync(CancellationToken cancellationToken = default)
        => dbContext.Users.ToListAsync(cancellationToken);

    public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => dbContext.Users.FirstOrDefaultAsync(x => x.id == id, cancellationToken);

    public Task<User?> GetByCredentialsAsync(string username, string password, CancellationToken cancellationToken = default)
        => dbContext.Users.FirstOrDefaultAsync(
            u => (u.username == username || u.email == username) && u.password == password,
            cancellationToken);

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => dbContext.Users.FirstOrDefaultAsync(u => u.email == email, cancellationToken);

    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
        => dbContext.Users.FirstOrDefaultAsync(u => u.username == username, cancellationToken);

    public async Task<bool> CreateUserAsync(User user, CancellationToken cancellationToken = default)
    {
        await dbContext.Users.AddAsync(user, cancellationToken);
        return await dbContext.SaveChangesAsync(cancellationToken) > 0;
    }

    public async Task<bool> UpdateUserAsync(User user, CancellationToken cancellationToken = default)
    {
        dbContext.Users.Update(user);
        return await dbContext.SaveChangesAsync(cancellationToken) > 0;
    }

    public async Task<bool> EnableTwoFactorAsync(int userId, string secret, string recoveryCodes, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.id == userId, cancellationToken);
        if (user is null) return false;

        user.isTwoFactorEnabled = true;
        user.twoFactorSecret = secret;
        user.twoFactorRecoveryCodes = recoveryCodes;

        return await dbContext.SaveChangesAsync(cancellationToken) > 0;
    }

    public async Task<bool> DisableTwoFactorAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.id == userId, cancellationToken);
        if (user is null) return false;

        user.isTwoFactorEnabled = false;
        user.twoFactorSecret = null;
        user.twoFactorRecoveryCodes = null;

        return await dbContext.SaveChangesAsync(cancellationToken) > 0;
    }

    public async Task<bool> UpdateRecoveryCodesAsync(int userId, string recoveryCodes, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.id == userId, cancellationToken);
        if (user is null) return false;

        user.twoFactorRecoveryCodes = recoveryCodes;
        return await dbContext.SaveChangesAsync(cancellationToken) > 0;
    }

    public Task<List<User>> GetUsersByLastLoginIpAsync(string ipAddress, CancellationToken cancellationToken = default)
        => dbContext.Users
            .Where(u => u.lastLoginIP == ipAddress && !u.isLocked)
            .ToListAsync(cancellationToken);

    public async Task<(IEnumerable<User> Items, int Total)> GetPagedAsync(
        string? keyword,
        bool? isApproved,
        bool? isLocked,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var q = dbContext.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            q = q.Where(x => (x.username ?? string.Empty).Contains(kw) || (x.email ?? string.Empty).Contains(kw));
        }

        if (isApproved.HasValue)
        {
            q = q.Where(x => x.isApproved == isApproved.Value);
        }

        if (isLocked.HasValue)
        {
            q = q.Where(x => x.isLocked == isLocked.Value);
        }

        var total = await q.CountAsync(cancellationToken);

        var items = await q
            .OrderByDescending(x => x.createdAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<bool> ApproveAsync(int id, CancellationToken cancellationToken = default)
    {
        var u = await GetByIdAsync(id, cancellationToken);
        if (u is null) return false;

        u.isApproved = true;
        return await dbContext.SaveChangesAsync(cancellationToken) > 0;
    }

    public async Task<bool> LockAsync(int id, string reason, CancellationToken cancellationToken = default)
    {
        var u = await GetByIdAsync(id, cancellationToken);
        if (u is null) return false;

        u.isLocked = true;
        u.lockedAt = DateTime.UtcNow;
        u.lockedReason = reason;

        return await dbContext.SaveChangesAsync(cancellationToken) > 0;
    }

    public async Task<bool> UnlockAsync(int id, CancellationToken cancellationToken = default)
    {
        var u = await GetByIdAsync(id, cancellationToken);
        if (u is null) return false;

        u.isLocked = false;
        u.lockedAt = null;
        u.lockedReason = null;

        return await dbContext.SaveChangesAsync(cancellationToken) > 0;
    }
}
