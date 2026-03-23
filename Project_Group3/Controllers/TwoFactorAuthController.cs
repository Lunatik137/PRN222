using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PRN222_Group3.Service;
using PRN222_Group3.Repository;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace PRN222_Group3.Controllers
{
    [Authorize]
    public class TwoFactorAuthController : Controller
    {
        private readonly ITwoFactorAuthService _twoFactorAuthService;
        private readonly UserRepository _userRepository;

        public TwoFactorAuthController(ITwoFactorAuthService twoFactorAuthService, UserRepository userRepository)
        {
            _twoFactorAuthService = twoFactorAuthService;
            _userRepository = userRepository;
        }

        // Helper method to get real client IP behind nginx reverse proxy
        private string GetClientIpAddress()
        {
            // Check X-Forwarded-For header first (nginx sets this)
            var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                // X-Forwarded-For can contain multiple IPs, take the first one (original client)
                return forwardedFor.Split(',')[0].Trim();
            }

            // Check X-Real-IP header (nginx also sets this)
            var realIp = HttpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            // Fallback to RemoteIpAddress
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        // GET: /TwoFactorAuth/Setup
        [HttpGet]
        public async Task<IActionResult> Setup()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Login");
            }

            var user = await _userRepository.GetUserByUsername(username);
            if (user == null)
            {
                return NotFound("User not found");
            }

            if (user.IsTwoFactorEnabled == true)
            {
                TempData["Message"] = "Two-factor authentication is already enabled for your account.";
                TempData["MessageType"] = "info";
                return RedirectToAction("UserDashboard", "UserDashboard");
            }

            var setupInfo = _twoFactorAuthService.GenerateSetupCode(user.Email ?? user.Username ?? "user");

            HttpContext.Session.SetString("TwoFactorSecret_Temp", setupInfo.secretKey);
            HttpContext.Session.SetInt32("TwoFactorUserId_Temp", user.Id);

            ViewBag.QrCodeImageUrl = setupInfo.qrCodeImageUrl;
            ViewBag.ManualEntryKey = setupInfo.manualEntryKey;
            ViewBag.UserEmail = user.Email ?? user.Username;

            return View();
        }

        // POST: /TwoFactorAuth/Enable
        [HttpPost]
        public async Task<IActionResult> Enable(string verificationCode)
        {
            var secretKey = HttpContext.Session.GetString("TwoFactorSecret_Temp");
            var userId = HttpContext.Session.GetInt32("TwoFactorUserId_Temp");

            if (string.IsNullOrEmpty(secretKey) || !userId.HasValue)
            {
                TempData["Message"] = "Session expired. Please start the setup process again.";
                TempData["MessageType"] = "error";
                return RedirectToAction("Setup");
            }

            if (string.IsNullOrWhiteSpace(verificationCode))
            {
                TempData["Message"] = "Please enter the verification code.";
                TempData["MessageType"] = "error";
                return RedirectToAction("Setup");
            }

            var result = await _twoFactorAuthService.EnableTwoFactorAsync(userId.Value, secretKey, verificationCode);

            if (result)
            {
                HttpContext.Session.Remove("TwoFactorSecret_Temp");
                HttpContext.Session.Remove("TwoFactorUserId_Temp");

                // Get recovery codes to display
                var user = await _userRepository.GetByIdAsync(userId.Value);
                if (user != null && !string.IsNullOrEmpty(user.TwoFactorRecoveryCodes))
                {
                    ViewBag.RecoveryCodes = user.TwoFactorRecoveryCodes.Split(',');
                    TempData["Message"] = "Two-factor authentication has been successfully enabled! Please save your recovery codes.";
                    TempData["MessageType"] = "success";
                    return View("RecoveryCodes");
                }

                TempData["Message"] = "Two-factor authentication has been successfully enabled!";
                TempData["MessageType"] = "success";
                return RedirectToAction("UserDashboard", "UserDashboard");
            }
            else
            {
                TempData["Message"] = "Invalid verification code. Please try again.";
                TempData["MessageType"] = "error";
                return RedirectToAction("Setup");
            }
        }

        // GET: /TwoFactorAuth/DisableRequest
        [HttpGet]
        public async Task<IActionResult> DisableRequest()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Login");
            }

            var user = await _userRepository.GetUserByUsername(username);
            if (user == null)
            {
                return NotFound("User not found");
            }

            if (user.IsTwoFactorEnabled != true)
            {
                TempData["Message"] = "Two-factor authentication is not enabled.";
                TempData["MessageType"] = "info";
                return RedirectToAction("UserDashboard", "UserDashboard");
            }

            return View();
        }

        // POST: /TwoFactorAuth/Disable
        [HttpPost]
        public async Task<IActionResult> Disable(string verificationCode)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Login");
            }

            var user = await _userRepository.GetUserByUsername(username);
            if (user == null)
            {
                return NotFound("User not found");
            }

            if (string.IsNullOrWhiteSpace(verificationCode))
            {
                TempData["Message"] = "Please enter the verification code.";
                TempData["MessageType"] = "error";
                return RedirectToAction("DisableRequest");
            }

            var result = await _twoFactorAuthService.DisableTwoFactorAsync(user.Id, verificationCode);

            if (result)
            {
                TempData["Message"] = "Two-factor authentication has been disabled.";
                TempData["MessageType"] = "success";
            }
            else
            {
                TempData["Message"] = "Invalid verification code. Please try again.";
                TempData["MessageType"] = "error";
                return RedirectToAction("DisableRequest");
            }

            return RedirectToAction("UserDashboard", "UserDashboard");
        }

        // GET: /TwoFactorAuth/Verify
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Verify()
        {
            var userId = HttpContext.Session.GetInt32("TwoFactorUserId_Login");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Login");
            }

            return View();
        }

        // POST: /TwoFactorAuth/VerifyCode
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> VerifyCode(string code, bool useRecoveryCode = false)
        {
            var userId = HttpContext.Session.GetInt32("TwoFactorUserId_Login");
            if (!userId.HasValue)
            {
                TempData["Message"] = "Session expired. Please login again.";
                TempData["MessageType"] = "error";
                return RedirectToAction("Login", "Login");
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                TempData["Message"] = "Please enter the verification code.";
                TempData["MessageType"] = "error";
                return RedirectToAction("Verify");
            }

            var user = await _userRepository.GetByIdAsync(userId.Value);
            if (user == null || string.IsNullOrEmpty(user.TwoFactorSecret))
            {
                TempData["Message"] = "Invalid session. Please login again.";
                TempData["MessageType"] = "error";
                return RedirectToAction("Login", "Login");
            }

            bool isValid = false;

            if (useRecoveryCode)
            {
                // Validate recovery code
                isValid = await _twoFactorAuthService.ValidateRecoveryCodeAsync(userId.Value, code);
                if (!isValid)
                {
                    TempData["Message"] = "Invalid recovery code. Please try again.";
                    TempData["MessageType"] = "error";
                    return RedirectToAction("Verify");
                }
            }
            else
            {
                // Validate 2FA code
                isValid = _twoFactorAuthService.ValidateTwoFactorPIN(user.TwoFactorSecret, code);
                if (!isValid)
                {
                    TempData["Message"] = "Invalid verification code. Please try again.";
                    TempData["MessageType"] = "error";
                    return RedirectToAction("Verify");
                }
            }

            if (isValid)
            {
                // Get session data BEFORE removing it
                var username = HttpContext.Session.GetString("TwoFactorUsername_Login");
                var role = HttpContext.Session.GetString("TwoFactorRole_Login");

                // Update last login IP and time
                var clientIp = GetClientIpAddress();
                user.LastLoginIp = clientIp;
                user.LastLoginTimestamp = DateTime.UtcNow;
                await _userRepository.UpdateUserAsync(user);

                // Clear 2FA session data
                HttpContext.Session.Remove("TwoFactorUserId_Login");
                HttpContext.Session.Remove("TwoFactorUsername_Login");
                HttpContext.Session.Remove("TwoFactorRole_Login");

                // Set regular session data
                HttpContext.Session.SetString("Id", user.Id.ToString());
                HttpContext.Session.SetString("Username", user.Username ?? "");
                HttpContext.Session.SetString("Role", user.Role ?? "");

                // Create authentication cookie
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username ?? ""),
                    new Claim(ClaimTypes.Role, user.Role ?? "")
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTime.UtcNow.AddHours(12)
                    });

                TempData["Message"] = "Login successful!";
                TempData["MessageType"] = "success";

                if (role == "SuperAdmin" || role == "Moderator" || role == "Support" || role == "Ops")
                {
                    return RedirectToAction("UserDashboard", "UserDashboard");
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                TempData["Message"] = "Invalid verification code. Please try again.";
                TempData["MessageType"] = "error";
                return RedirectToAction("Verify");
            }
        }
    }
}
