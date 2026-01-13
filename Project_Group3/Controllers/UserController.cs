using Microsoft.AspNetCore.Mvc;
using Project_Group3.Models;
using Project_Group3.Repository.Interfaces;

namespace Project_Group3.Controllers
{
    [Route("api/user")]
    public class UserController : Controller
    {
        private readonly IUserRepository _userRepository;
        public UserController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }
        [HttpGet]
        public async Task<User?> GetById(int id)
        => (await _userRepository.GetByIdAsync(id));
    }
}
