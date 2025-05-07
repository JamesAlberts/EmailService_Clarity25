namespace EmailService_Clarity25.API.Services
{
    using EmailService_Clarity25.API.Data;
    using EmailService_Clarity25.API.Models;
    using MailKit.Net.Smtp;
    using MailKit.Security;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using MimeKit;

    public class EmailSender : IEmailSender
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailSender> _logger;
        private readonly EmailLogDbContext _dbContext;

        public EmailSender(
            IOptions<EmailSettings> emailSettings,
            ILogger<EmailSender> logger,
            EmailLogDbContext dbContext)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task SendEmailAsync(string recipient, string subject, string body)
        {
            if (string.IsNullOrEmpty(recipient) || !IsValidEmail(recipient))
            {
                throw new ArgumentException("Recipient email is invalid");
            }

            var emailLog = new EmailLog
            {
                Sender = _emailSettings.SenderEmail,
                Recipient = recipient,
                Subject = subject,
                Body = body,
                SentDate = DateTime.UtcNow,
                Status = "Pending",
                Attempts = 0,
                ErrorMessage = ""
            };

            _dbContext.EmailLogs.Add(emailLog);
            await _dbContext.SaveChangesAsync();

            int maxRetries = 3;
            int attempt = 0;
            bool success = false;

            while (attempt < maxRetries && !success)
            {
                attempt++;
                emailLog.Attempts = attempt;
                emailLog.Status = attempt > 1 ? "Retrying" : "Pending";

                try
                {
                    var message = new MimeMessage();
                    message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
                    message.To.Add(new MailboxAddress(recipient, recipient));
                    message.Subject = subject;
                    message.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = body };

                    using (var client = new SmtpClient())
                    {
                        await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, SecureSocketOptions.StartTls);
                        client.Authenticate(_emailSettings.Username, _emailSettings.Password);
                        await client.SendAsync(message);
                        await client.DisconnectAsync(true);
                    }

                    success = true;
                    emailLog.Status = "Sent";
                    _logger.LogInformation($"Email sent successfully to {recipient} on attempt {attempt}");
                }
                catch (Exception ex)
                {
                    emailLog.Status = "Failed";
                    emailLog.ErrorMessage = ex.Message;
                    _logger.LogError(ex, $"Failed to send email to {recipient} on attempt {attempt}", ex.Message);
                    if (attempt < maxRetries)
                    {
                        await Task.Delay(attempt * 1000);
                    }
                }
                finally
                {
                    _dbContext.Entry(emailLog).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                    await _dbContext.SaveChangesAsync();
                }
            }

            if (!success)
            {
                _logger.LogError($"Email sending failed after {maxRetries} attempts to {recipient}.");
                throw new Exception($"Email sending failed after {maxRetries} attempts to {recipient}");
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
