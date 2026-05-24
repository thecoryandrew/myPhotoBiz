using Microsoft.AspNetCore.Identity.UI.Services;

namespace MyPhotoBiz.Services
{
    // Stub implementation. Replace with a real provider (SendGrid, SMTP, SES, etc.) for production.
    public class EmailSender : IEmailSender
    {
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(ILogger<EmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            _logger.LogInformation("Email (stub) to {Email} - {Subject}", email, subject);
            return Task.CompletedTask;
        }
    }
}
