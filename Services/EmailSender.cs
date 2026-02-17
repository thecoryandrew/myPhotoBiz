using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;

namespace MyPhotoBiz.Services
{
    public class EmailSettings
    {
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public string SmtpUsername { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = "MyPhotoBiz";
        public bool EnableSsl { get; set; } = true;
    }

    // TODO: [HIGH] Add email template system with customizable branding
    // TODO: [HIGH] Add the following automated emails:
    //       - Client welcome email on account creation (with temp password)
    //       - Booking confirmation/decline notifications
    //       - Invoice sent notification with payment link
    //       - Payment reminder (3 days before due date)
    //       - Overdue invoice notice (1, 7, 14 days)
    //       - Gallery ready notification with access link
    //       - Contract sent for signature notification
    //       - Payment received confirmation
    //       - PhotoShoot reminder (24 hours before)
    // TODO: [MEDIUM] Add email queue for async sending (Hangfire)
    // TODO: [MEDIUM] Add email tracking (opened, clicked)
    // TODO: [FEATURE] Add SMS notification support
    // TODO: [FEATURE] Add unsubscribe management
    public class EmailSender : IEmailSender
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailSender> _logger;
        private readonly IWebHostEnvironment _environment;

        public EmailSender(IOptions<EmailSettings> settings, ILogger<EmailSender> logger, IWebHostEnvironment environment)
        {
            _settings = settings.Value;
            _logger = logger;
            _environment = environment;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // In development without SMTP configured, log instead of sending
            if (string.IsNullOrEmpty(_settings.SmtpHost))
            {
                _logger.LogInformation("========== EMAIL (no SMTP configured) ==========");
                _logger.LogInformation("To: {Email}", email);
                _logger.LogInformation("Subject: {Subject}", subject);
                _logger.LogInformation("Message: {Message}", htmlMessage);
                _logger.LogInformation("=================================================");
                return;
            }

            try
            {
                using var message = new MailMessage();
                message.From = new MailAddress(_settings.FromEmail, _settings.FromName);
                message.To.Add(new MailAddress(email));
                message.Subject = subject;
                message.Body = htmlMessage;
                message.IsBodyHtml = true;

                using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort);
                client.EnableSsl = _settings.EnableSsl;

                if (!string.IsNullOrEmpty(_settings.SmtpUsername))
                {
                    client.Credentials = new NetworkCredential(_settings.SmtpUsername, _settings.SmtpPassword);
                }

                await client.SendMailAsync(message);
                _logger.LogInformation("Email sent successfully to {Email} with subject '{Subject}'", email, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email} with subject '{Subject}'", email, subject);
                throw;
            }
        }
    }
}
