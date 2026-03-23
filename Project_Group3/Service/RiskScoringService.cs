using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PRN222_Group3.Models;

namespace PRN222_Group3.Service
{
    /// <summary>
    /// Simple Risk Scoring Service
    /// </summary>
    public class RiskScoringService
    {
        private readonly CloneEbayDbContext _context;
        private readonly IEmailService _emailService;

        private static readonly string[] DisposableEmailDomains = new[]
        {
            "tempmail.com", "guerrillamail.com", "10minutemail.com", "throwaway.email",
            "mailinator.com", "temp-mail.org", "trashmail.com", "yopmail.com"
        };

        public RiskScoringService(CloneEbayDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        /// <summary>
        /// Calculate risk score for new registration
        /// </summary>
        public async Task<RiskAssessment> CalculateRiskScoreAsync(User newUser, string registrationIp)
        {
            var assessment = new RiskAssessment
            {
                UserId = newUser.Id,
                AssessmentIpAddress = registrationIp,
                AssessmentDate = DateTime.UtcNow
            };
            int score = 0;

            // Check IP match with existing accounts
            var existingUsersWithIp = await _context.Users
                .Where(u => u.LastLoginIp == registrationIp && u.Id != newUser.Id && !u.IsLocked)
                .ToListAsync();

            if (existingUsersWithIp.Any())
            {
                assessment.IpMatchWithExistingAccount = true;
                assessment.ExistingAccountsWithSameIp = existingUsersWithIp.Count;
                score += 30; // Moderate risk
            }

            // Account age is always 0 at registration, so this check is not useful here
            // Keep the field for future login-based risk assessments
            var accountAge = DateTime.UtcNow - newUser.CreatedAt;
            assessment.NewAccount = true; // Always true for new registrations
            assessment.DaysSinceRegistration = (int)accountAge.TotalDays;

            // Check email domain similarity (business/family indicator)
            if (existingUsersWithIp.Any())
            {
                var newEmailDomain = newUser.Email?.Split('@').LastOrDefault()?.ToLower();
                var existingEmailDomains = existingUsersWithIp
                    .Select(u => u.Email?.Split('@').LastOrDefault()?.ToLower())
                    .Where(d => !string.IsNullOrEmpty(d))
                    .ToList();

                if (existingEmailDomains.Any(d => d == newEmailDomain))
                {
                    assessment.SameEmailDomain = true;
                    score -= 20; // Reduce risk - likely same business/family
                }
            }

            // Check business hours
            var currentHour = DateTime.Now.Hour;
            if (currentHour < 9 || currentHour > 17)
            {
                assessment.OutsideBusinessHours = true;
                score += 10;
            }

            // Check disposable email
            //var emailDomain = newUser.Email?.Split('@').LastOrDefault()?.ToLower();
            var emailDomain = "gmail.com";
            if (!string.IsNullOrEmpty(emailDomain) && DisposableEmailDomains.Contains(emailDomain))
            {
                assessment.DisposableEmail = true;
                score += 50;
            }

            // Check rapid registrations (last 24 hours)
            var recentRegistrations = await _context.Users
                .Where(u => u.RegistrationIp == registrationIp && u.CreatedAt >= DateTime.UtcNow.AddHours(-24))
                .CountAsync();

            if (recentRegistrations > 3)
            {
                assessment.RapidRegistrations = true;
                score += 20;
            }

            // Cap score at 100
            assessment.RiskScore = Math.Min(Math.Max(score, 0), 100);

            // Determine risk level and recommended action
            DetermineRiskLevelAndAction(assessment);

            // Save to database
            _context.RiskAssessments.Add(assessment);

            // Update user's risk fields
            newUser.RiskScore = assessment.RiskScore;
            newUser.RiskLevel = assessment.RiskLevel;
            newUser.LastRiskAssessment = assessment.AssessmentDate;

            await _context.SaveChangesAsync();

            // Send notification to existing users with same IP
            if (assessment.IpMatchWithExistingAccount)
            {
                await NotifyExistingUsersAsync(registrationIp, newUser.Email);
            }

            // Log assessment result
            Console.WriteLine($"[Risk Assessment] User: {newUser.Username}, Score: {assessment.RiskScore}, " +
                            $"Level: {assessment.RiskLevel}, Action: {assessment.RecommendedAction}");

            return assessment;
        }

        /// <summary>
        /// Determine risk level and recommended action based on score
        /// </summary>
        private void DetermineRiskLevelAndAction(RiskAssessment assessment)
        {
            if (assessment.RiskScore >= 70)
            {
                assessment.RiskLevel = "Critical";
                assessment.RecommendedAction = "Block";
                assessment.Reason = "High fraud risk detected. Manual review required before approval.";
            }
            else if (assessment.RiskScore >= 50)
            {
                assessment.RiskLevel = "High";
                assessment.RecommendedAction = "Review";
                assessment.Reason = "Elevated risk. Additional verification recommended.";
            }
            else if (assessment.RiskScore >= 30)
            {
                assessment.RiskLevel = "Medium";
                assessment.RecommendedAction = "Monitor";
                assessment.Reason = "Moderate risk. Allow registration but monitor activity.";
            }
            else
            {
                assessment.RiskLevel = "Low";
                assessment.RecommendedAction = "Allow";
                assessment.Reason = "Low risk. Normal registration flow.";
            }
        }

        /// <summary>
        /// Notify existing users about new registration from their IP
        /// </summary>
        private async Task NotifyExistingUsersAsync(string ipAddress, string? newUserEmail)
        {
            var existingUsers = await _context.Users
                .Where(u => u.LastLoginIp == ipAddress && !u.IsLocked)
                .ToListAsync();

            foreach (var user in existingUsers)
            {
                if (!string.IsNullOrEmpty(user.Email))
                {
                    try
                    {
                        await _emailService.SendEmailAsync(user.Email,
                            "Security Notice: New Account Registration from Your IP",
                            $@"
                                <h2>Security Notice</h2>
                                <p>Hello {user.Username},</p>
                                <p>A new account was registered from an IP address you recently used.</p>
                                <p><strong>Details:</strong></p>
                                <ul>
                                    <li>IP Address: {ipAddress}</li>
                                    <li>Date: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</li>
                                </ul>
                                <p>If this was you, you can safely ignore this message.</p>
                                <p>If you didn't create a new account, please review your account security.</p>
                                <p>Thank you,<br>Security Team</p>
                            ");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to send notification email: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Get risk assessment history for a user
        /// </summary>
        public async Task<List<RiskAssessment>> GetUserRiskHistoryAsync(int userId)
        {
            return await _context.RiskAssessments
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.AssessmentDate)
                .ToListAsync();
        }

        /// <summary>
        /// Get latest risk assessment for a user
        /// </summary>
        public async Task<RiskAssessment?> GetLatestRiskAssessmentAsync(int userId)
        {
            return await _context.RiskAssessments
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.AssessmentDate)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Send verification request email for high-risk users
        /// </summary>
        public async Task SendVerificationRequestEmailAsync(User user, int riskScore)
        {
            if (string.IsNullOrEmpty(user.Email))
                return;

            try
            {
                await _emailService.SendEmailAsync(user.Email,
                    "Account Verification Required - Additional Documents Needed",
                    $@"
                        <h2>Account Verification Required</h2>
                        <p>Hello {user.Username},</p>
                        <p>Thank you for registering with us. Due to security measures, we need to verify your account before it can be approved.</p>
                        <p><strong>Risk Assessment:</strong></p>
                        <ul>
                            <li>Risk Score: {riskScore}/100</li>
                            <li>Status: High Risk - Requires Verification</li>
                        </ul>
                        <p><strong>Required Documents:</strong></p>
                        <ul>
                            <li>Government-issued photo ID (passport, driver's license, or national ID card)</li>
                            <li>Proof of address (utility bill, bank statement, or official document dated within last 3 months)</li>
                            <li>Selfie holding your ID document</li>
                        </ul>
                        <p><strong>How to Submit:</strong></p>
                        <p>Please reply to this email with clear photos or scans of the required documents. Our admin team will review your submission and approve your account within 24-48 hours.</p>
                        <p><strong>Important Notes:</strong></p>
                        <ul>
                            <li>Your account is currently pending and cannot be used until verification is complete</li>
                            <li>All documents must be valid and clearly readable</li>
                            <li>Your information will be kept confidential and used only for verification purposes</li>
                        </ul>
                        <p>If you have any questions, please contact our support team.</p>
                        <p>Thank you for your cooperation,<br>Security & Compliance Team</p>
                    ");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send verification request email: {ex.Message}");
            }
        }
    }
}
