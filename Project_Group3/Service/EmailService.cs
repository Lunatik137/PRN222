using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PRN222_Group3.Service
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string toEmail, string subject, string body);
        Task<bool> SendBulkEmailAsync(List<string> toEmails, string subject, string body);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        private bool ShouldSimulateOnly()
        {
            var v = _configuration["EmailSettings:SimulateEmailSending"];
            return string.Equals(v, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(v, "1", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                if (ShouldSimulateOnly())
                {
                    _logger.LogWarning(
                        "[SimulateEmail] To={To} | Subject={Subject} | BodyLength={Len}",
                        toEmail, subject, body?.Length ?? 0);
                    return true;
                }

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(
                    _configuration["EmailSettings:SenderName"] ?? "PRN222 Group3",
                    _configuration["EmailSettings:SenderEmail"] ?? "fpthemath@gmail.com"
                ));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = subject;

                var builder = new BodyBuilder
                {
                    HtmlBody = body
                };
                message.Body = builder.ToMessageBody();

                using var client = new SmtpClient();
                
                // For development/testing, you might use a test SMTP server
                // For production, configure proper SMTP settings in appsettings.json
                var smtpHost = _configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var smtpUser = _configuration["EmailSettings:SmtpUser"];
                var smtpPass = _configuration["EmailSettings:SmtpPassword"];

                await client.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                
                if (!string.IsNullOrEmpty(smtpUser) && !string.IsNullOrEmpty(smtpPass))
                {
                    await client.AuthenticateAsync(smtpUser, smtpPass);
                }
                
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email sending failed for {To}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendBulkEmailAsync(List<string> toEmails, string subject, string body)
        {
            var tasks = toEmails.Select(email => SendEmailAsync(email, subject, body));
            var results = await Task.WhenAll(tasks);
            return results.All(r => r);
        }
    }
}
