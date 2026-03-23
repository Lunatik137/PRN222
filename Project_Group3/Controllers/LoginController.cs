using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
//using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PRN222_Group3.Repository;
using PRN222_Group3.Service;
using PRN222_Group3.Models;

namespace OnlineMarketPlace.Controllers
{

    public class LoginController : Controller
    {
        private readonly ILogger<LoginController> _logger;

        private readonly IEmailService _emailService;

        private readonly IUserService _userService;
        private readonly UserRepository _userRepository;
        private readonly RiskScoringService _riskScoringService;

        public LoginController(ILogger<LoginController> logger, IUserService userService, IEmailService emailService, RiskScoringService riskScoringService)
        {
            _logger = logger;
            _userService = userService;
            _emailService = emailService;
            _userRepository = new UserRepository();
            _riskScoringService = riskScoringService;
        }


        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> CheckLogin(string username, string password)
        {
            // First check if user exists with these credentials
            var checkUser = await _userRepository.GetUserByUsername(username);

            if (checkUser == null)
            {
                TempData["Message"] = "Username or password is incorrect!";
                TempData["MessageType"] = "error";
                return View("Login");
            }

            if (!checkUser.IsApproved)
            {
                TempData["Message"] = "Your account is pending approval. Please wait for administrator approval.";
                TempData["MessageType"] = "error";
                return View("Login");
            }

            if (checkUser.IsLocked)
            {
                TempData["Message"] = "Your account has been locked. Reason: " + checkUser.LockedReason;
                TempData["MessageType"] = "error";
                return View("Login");
            }

            var user = await _userService.AuthenticateAsync(username, password);
            if (user == null)
            {
                TempData["Message"] = "Username or password is incorrect!";
                TempData["MessageType"] = "error";
                return View("Login");
            }

            if (user.IsTwoFactorEnabled == true && !string.IsNullOrEmpty(user.TwoFactorSecret))
            {
                HttpContext.Session.SetInt32("TwoFactorUserId_Login", user.Id);
                HttpContext.Session.SetString("TwoFactorUsername_Login", user.Username ?? "");
                HttpContext.Session.SetString("TwoFactorRole_Login", user.Role ?? "");

                return RedirectToAction("Verify", "TwoFactorAuth");
            }

            HttpContext.Session.SetString("Id", user.Id.ToString());
            HttpContext.Session.SetString("Username", user.Username ?? "");
            HttpContext.Session.SetString("Role", user.Role ?? "");

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

            if (user.Role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase)
                || user.Role.Equals("Moderator", StringComparison.OrdinalIgnoreCase)
                || user.Role.Equals("Support", StringComparison.OrdinalIgnoreCase)
                || user.Role.Equals("Ops", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("UserDashboard", "UserDashboard");
            }
            else
            {
                if (user.Role.Equals("Buyer", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction("Index", "Customer");
                }
                return RedirectToAction("Index", "Seller");
            }
        }



        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

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


        [HttpPost]
        public async Task<IActionResult> Register(string username, string email, string password, string fullName, string phone, string role)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["Message"] = "Please fill in all required fields.";
                TempData["MessageType"] = "error";
                return View();
            }

            // Check if username exists
            var existingUsername = await _userRepository.GetUserByUsername(username);
            if (existingUsername != null)
            {
                TempData["Message"] = "Username is already taken.";
                TempData["MessageType"] = "error";
                return View();
            }

            // Check if email exists
            var existingEmail = await _userRepository.GetUserByEmail(email);
            if (existingEmail != null)
            {
                TempData["Message"] = "Email is already registered.";
                TempData["MessageType"] = "error";
                return View();
            }

            // Get client IP address
            var clientIp = GetClientIpAddress();

            var user = new User
            {
                Username = username,
                Email = email,
                Password = password,
                Role = role,
                IsApproved = false,
                IsLocked = false,
                CreatedAt = DateTime.UtcNow,
                IsTwoFactorEnabled = false,
                AvatarUrl = "/images/default-avatar.png",
                RegistrationIp = clientIp,
                Phone = phone
            };

            var success = await _userService.CreateUserAsync(user);
            if (!success)
            {
                TempData["Message"] = "An error occurred during registration. Please try again.";
                TempData["MessageType"] = "error";
                return View();
            }

            // At this point, user.Id is populated by EF Core after SaveChangesAsync
            // Risk Scoring: Calculate fraud risk and determine action
            try
            {
                var assessment = await _riskScoringService.CalculateRiskScoreAsync(user, clientIp);

                // Log risk assessment
                _logger.LogInformation("Registration Risk Assessment - User: {Username}, Score: {Score}, Level: {Level}, Action: {Action}",
                    username, assessment.RiskScore, assessment.RiskLevel, assessment.RecommendedAction);

                // Handle based on risk score
                if (assessment.RiskScore >= 70)
                {
                    // Critical Risk: Send verification email requesting ID documents
                    await _riskScoringService.SendVerificationRequestEmailAsync(user, assessment.RiskScore);

                    TempData["Message"] = $"Registration received. High risk detected (Score: {assessment.RiskScore}). Please check your email for verification instructions. You must submit proof of identity documents for admin approval.";
                    TempData["MessageType"] = "warning";
                }
                else if (assessment.RiskScore >= 50)
                {
                    // High Risk: Require phone number verification
                    if (string.IsNullOrWhiteSpace(phone))
                    {
                        TempData["Message"] = $"Elevated risk detected (Score: {assessment.RiskScore}). Please provide a valid phone number for verification.";
                        TempData["MessageType"] = "warning";
                        TempData["RequirePhone"] = true;
                        return View();
                    }

                    TempData["Message"] = $"Registration successful! Elevated risk detected (Score: {assessment.RiskScore}). Your account is pending admin approval.";
                    TempData["MessageType"] = "info";
                }
                else
                {
                    // Low Risk: Auto-approve
                    user.IsApproved = true;
                    await _userRepository.UpdateUserAsync(user);

                    TempData["Message"] = "Registration successful! Your account has been approved. You can now log in.";
                    TempData["MessageType"] = "success";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during risk scoring for user {Username}", username);
                TempData["Message"] = "Registration successful! Your account is pending approval from an administrator.";
                TempData["MessageType"] = "success";
            }

            return RedirectToAction("Login");
        }
    }
}
