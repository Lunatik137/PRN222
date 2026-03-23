
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRN222_Group3.Models;

namespace PRN222_Group3.Repository
{
    public class UserRepository
    {
        private CloneEbayDbContext _context;

        public UserRepository()
        {
            _context = new CloneEbayDbContext();
        }


        public List<User> GetUsers()
        {
            _context = new CloneEbayDbContext();
            return _context.Users.ToList();
        }

        public async Task<User?> GetUser(string username, string password)
        {
            _context = new CloneEbayDbContext();
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username && u.Password == password);
        }

        public async Task<User?> GetUserByEmail(string email)
        {
            _context = new CloneEbayDbContext();
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetUserByUsername(string username)
        {
            _context = new CloneEbayDbContext();
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        public virtual async Task<bool> UpdateUserAsync(User user)
        {
            try
            {
                _context = new CloneEbayDbContext();
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> EnableTwoFactorAsync(int userId, string secret, string recoveryCodes)
        {
            _context = new CloneEbayDbContext();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return false;

            user.IsTwoFactorEnabled = true;
            user.TwoFactorSecret = secret;
            user.TwoFactorRecoveryCodes = recoveryCodes;
            return await _context.SaveChangesAsync() > 0;
        }

         public async Task<List<User>> GetUsersByLastLoginIpAsync(string ipAddress)
        {
            _context = new CloneEbayDbContext();
            return await _context.Users
                .Where(u => u.LastLoginIp == ipAddress && !u.IsLocked)
                .ToListAsync();
        }

        public async Task<bool> DisableTwoFactorAsync(int userId)
        {
            _context = new CloneEbayDbContext();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return false;

            user.IsTwoFactorEnabled = false;
            user.TwoFactorSecret = null;
            user.TwoFactorRecoveryCodes = null;
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateRecoveryCodesAsync(int userId, string recoveryCodes)
        {
            _context = new CloneEbayDbContext();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return false;

            user.TwoFactorRecoveryCodes = recoveryCodes;
            return await _context.SaveChangesAsync() > 0;
        }

        public virtual async Task<(IEnumerable<User> Items, int Total)> GetPagedAsync(
            string? keyword, bool? isApproved, bool? isLocked,
            int page, int pageSize)
        {
            var q = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim();
                q = q.Where(x => x.Username.Contains(kw) || x.Email.Contains(kw));
            }
            if (isApproved.HasValue) q = q.Where(x => x.IsApproved == isApproved.Value);
            if (isLocked.HasValue) q = q.Where(x => x.IsLocked == isLocked.Value);

            var total = await q.CountAsync();

            var items = await q.OrderByDescending(x => x.CreatedAt)
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .ToListAsync();

            return (items, total);
        }

        // Lấy theo Id
        public virtual Task<User?> GetByIdAsync(int id) =>
            _context.Users.FirstOrDefaultAsync(x => x.Id == id);

        // Duyệt tài khoản
        public virtual async Task<bool> ApproveAsync(int id)
        {
            var u = await GetByIdAsync(id);
            if (u is null) return false;
            u.IsApproved = true;
            return await _context.SaveChangesAsync() > 0;
        }

        // Khoá tài khoản
        public virtual async Task<bool> LockAsync(int id, string reason)
        {
            var u = await GetByIdAsync(id);
            if (u is null) return false;
            u.IsLocked = true;
            u.LockedAt = DateTime.UtcNow;
            u.LockedReason = reason;
            return await _context.SaveChangesAsync() > 0;
        }

        // Mở khoá tài khoản
        public virtual async Task<bool> UnlockAsync(int id)
        {
            var u = await GetByIdAsync(id);
            if (u is null) return false;
            u.IsLocked = false;
            u.LockedAt = null;
            u.LockedReason = null;
            return await _context.SaveChangesAsync() > 0;
        }

        // Create new user
        public virtual async Task<bool> CreateUserAsync(User user)
        {
            try
            {
                _context = new CloneEbayDbContext();
                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}

