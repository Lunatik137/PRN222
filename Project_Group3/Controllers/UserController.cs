using Microsoft.AspNetCore.Mvc;
using Project_Group3.Models;
using Project_Group3.Repository.Interfaces;
using Project_Group3.Services;
namespace Project_Group3.Controllers
{
    [ApiController]
    [Route("api/user")]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasherService _passwordHasherService;

        public UserController(IUserRepository userRepository, IPasswordHasherService passwordHasherService)
        {
            _userRepository = userRepository;
            _passwordHasherService = passwordHasherService;
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<User>> GetById(int id, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(id, cancellationToken);
            return user is null ? NotFound() : Ok(user);
        }

        [HttpGet("all")]
        public Task<List<User>> GetAll(CancellationToken cancellationToken)
            => _userRepository.GetUsersAsync(cancellationToken);

        public sealed record LoginRequest(string Username, string Password);

        [HttpPost("login")]
        public async Task<ActionResult<User>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByCredentialsAsync(request.Username, request.Password, cancellationToken);
            if (user is null || user.isLocked || !user.isApproved)
            {
                return Unauthorized();
            }

            return Ok(user);
        }

        [HttpGet("by-email")]
        public async Task<ActionResult<User>> GetByEmail([FromQuery] string email, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
            return user is null ? NotFound() : Ok(user);
        }

        [HttpGet("by-username")]
        public async Task<ActionResult<User>> GetByUsername([FromQuery] string username, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByUsernameAsync(username, cancellationToken);
            return user is null ? NotFound() : Ok(user);
        }

        [HttpGet("paged")]
        public async Task<ActionResult<object>> GetPaged(
            [FromQuery] string? keyword,
            [FromQuery] bool? isApproved,
            [FromQuery] bool? isLocked,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 20;

            var (items, total) = await _userRepository.GetPagedAsync(keyword, isApproved, isLocked, page, pageSize, cancellationToken);
            return Ok(new { items, total, page, pageSize });
        }

        [HttpPost("approve/{id:int}")]
        public async Task<IActionResult> Approve(int id, CancellationToken cancellationToken)
            => await _userRepository.ApproveAsync(id, cancellationToken) ? NoContent() : NotFound();

        public sealed record LockRequest(string Reason);

        [HttpPost("reject/{id:int}")]
        public async Task<IActionResult> Reject(int id, [FromBody] LockRequest request, CancellationToken cancellationToken)
            => await _userRepository.RejectAsync(id, request.Reason, cancellationToken) ? NoContent() : NotFound();

        [HttpPost("lock/{id:int}")]
        public async Task<IActionResult> Lock(int id, [FromBody] LockRequest request, CancellationToken cancellationToken)
            => await _userRepository.LockAsync(id, request.Reason, cancellationToken) ? NoContent() : NotFound();

        [HttpPost("unlock/{id:int}")]
        public async Task<IActionResult> Unlock(int id, CancellationToken cancellationToken)
            => await _userRepository.UnlockAsync(id, cancellationToken) ? NoContent() : NotFound();

        public sealed record TwoFactorEnableRequest(string Secret, string RecoveryCodes);

        [HttpPost("2fa/enable/{id:int}")]
        public async Task<IActionResult> EnableTwoFactor(int id, [FromBody] TwoFactorEnableRequest request, CancellationToken cancellationToken)
            => await _userRepository.EnableTwoFactorAsync(id, request.Secret, request.RecoveryCodes, cancellationToken) ? NoContent() : NotFound();

        [HttpPost("2fa/disable/{id:int}")]
        public async Task<IActionResult> DisableTwoFactor(int id, CancellationToken cancellationToken)
            => await _userRepository.DisableTwoFactorAsync(id, cancellationToken) ? NoContent() : NotFound();

        [HttpPost("2fa/recovery-codes/{id:int}")]
        public async Task<IActionResult> UpdateRecoveryCodes(int id, [FromBody] string recoveryCodes, CancellationToken cancellationToken)
            => await _userRepository.UpdateRecoveryCodesAsync(id, recoveryCodes, cancellationToken) ? NoContent() : NotFound();

        [HttpGet("by-last-login-ip")]
        public Task<List<User>> GetByLastLoginIp([FromQuery] string ipAddress, CancellationToken cancellationToken)
            => _userRepository.GetUsersByLastLoginIpAsync(ipAddress, cancellationToken);

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] User user, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(user.password))
            {
                user.password = _passwordHasherService.HashPassword(user.password);
            }

            return await _userRepository.CreateUserAsync(user, cancellationToken) ? Ok() : BadRequest();
        }

        public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

        [HttpPost("change-password/{id:int}")]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(id, cancellationToken);
            if (user?.password is null)
            {
                return NotFound();
            }

            if (!_passwordHasherService.VerifyPassword(user.password, request.CurrentPassword))
            {
                return Unauthorized();
            }

            user.password = _passwordHasherService.HashPassword(request.NewPassword);
            return await _userRepository.UpdateUserAsync(user, cancellationToken) ? NoContent() : BadRequest();
        }

        [HttpPut("update")]
        public async Task<IActionResult> Update([FromBody] User user, CancellationToken cancellationToken)
            => await _userRepository.UpdateUserAsync(user, cancellationToken) ? NoContent() : BadRequest();
    }
}