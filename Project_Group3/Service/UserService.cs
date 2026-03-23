using PRN222_Group3.Models;
using PRN222_Group3.Repository;

namespace PRN222_Group3.Service
{
    public class UserService : IUserService
    {
        private readonly UserRepository _userRepository;

        public UserService(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public Task<User?> AuthenticateAsync(string username, string password)
        {
            return _userRepository.GetUser(username, password);
        }

        public Task<(IEnumerable<User> Items, int Total)> GetPagedAsync(string? keyword, bool? isApproved, bool? isLocked, int page, int pageSize)
        {
            return _userRepository.GetPagedAsync(keyword, isApproved, isLocked, page, pageSize);
        }

        public Task<User?> GetByIdAsync(int id)
        {
            return _userRepository.GetByIdAsync(id);
        }

        public Task<bool> ApproveAsync(int id)
        {
            return _userRepository.ApproveAsync(id);
        }

        public Task<bool> LockAsync(int id, string reason)
        {
            return _userRepository.LockAsync(id, reason);
        }

        public Task<bool> UnlockAsync(int id)
        {
            return _userRepository.UnlockAsync(id);
        }

        public Task<bool> CreateUserAsync(User user)
        {
            return _userRepository.CreateUserAsync(user);
        }
    }
}
