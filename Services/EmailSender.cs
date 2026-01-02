using Microsoft.AspNetCore.Identity.UI.Services;

namespace MyPhotoBiz.Services
{
    // TODO: [CRITICAL] This is a stub - integrate production email service (SendGrid, Mailgun, AWS SES)
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
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(ILogger<EmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // For development: log email to console instead of sending
            _logger.LogInformation("========== EMAIL ==========");
            _logger.LogInformation("To: {Email}", email);
            _logger.LogInformation("Subject: {Subject}", subject);
            _logger.LogInformation("Message: {Message}", htmlMessage);
            _logger.LogInformation("===========================");

            // TODO: In production, integrate with an actual email service (SendGrid, SMTP, etc.)
            return Task.CompletedTask;
        }
    }
}
