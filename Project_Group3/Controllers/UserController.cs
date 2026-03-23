using Microsoft.EntityFrameworkCore;
using PRN222_Group3.Models;

namespace PRN222_Group3.Controllers
{
    public class UserController
    {
        private readonly CloneEbayDbContext _context;

        private async Task<int> CountAccount()
        {
            return await _context.Users.CountAsync();
        }
    }
}
