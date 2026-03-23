using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;

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

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
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
                // Log the exception
                Console.WriteLine($"Email sending failed: {ex.Message}");
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
