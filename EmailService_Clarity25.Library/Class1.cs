using System.ComponentModel.DataAnnotations;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace EmailService_Clarity25.Library
{
    public class EmailSettings
    {
        [Required(ErrorMessage = "Smtp Server is required")]
        public string SmtpServer { get; set; }

        [Required(ErrorMessage = "Smtp Port is Required")]
        public int Smtport { get; set; }

        [Required(ErrorMessage = "Username is missing, this is required.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is mission, this is required.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "The Sender's Email is required.")]
        public string SenderEmail { get; set; }

        [Required(ErrorMessage = "The sender's name is required")]
        public string SenderName { get; set; }
    }

    public class EmailLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Sender { get; set; }

        [Required]
        public string Recipient { get; set; }

        [Required]
        public string Subject { get; set; }

        [Required]
        public string Body { get; set; }

        [Required]
        public DateTime SentDate { get; set; }

        [Required
            ]
        public string Status { get; set; } // the possibilites are: Pending, Failed, Rery, Sent
        public int Attempts { get; set; }
        public string ErrorMessage { get; set; } // if any
    }

    // database session
    public class EmailLogDbContext : DbContext
    {
        public EmailLogDbContext(DbContextOptions<EmailLogDbContext> options) : base(options) { }

        public DbSet<EmailLog> EmailLogs { get; set; }

        // the API configuration
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EmailLog>()
                .Property(e => e.Status)
                .HasMaxLength(20)
                .IsRequired();
            modelBuilder.Entity<EmailLog>()
                .Property(e => e.ErrorMessage)
                .HasMaxLength(255); // of course we wont have any errors at all, right?!?
        }
    }

    // email interface
    public interface IEmailSender
    {
        Task SendEmailAsync(string recipient, string subject, string body);
    }

    public class EmailSender : IEmailSender
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailSender> _logger;
        private readonly EmailLogDbContext _dbContext;

        // constructor
        public EmailSender(IOptions<EmailSettings> emailSettings, ILogger<EmailSender> logger, EmailLogDbContext dbContext)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
            _dbContext = dbContext;
        }

        // we dont want the process of sending and email to stop anything else, so set async
        public async Task SendEmailAsync(string recipient, string subject, string body)
        {
            // check for valid recipeint
            if (string.IsNullOrEmpty(recipient) || !isValidEmail(recipient))
            {
                throw new ArgumentException("Recipeint email is invalid");
            }

            // create long entry into the database
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
            await _dbContext.SaveChangesAsync(); // save the log entry

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
                    // lets try and send us a message
                    var message = new MimeMessage();
                    message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
                    message.To.Add(new MailboxAddress(recipient, recipient));
                    message.Subject = subject;
                    message.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = body };

                    using (var client = new SmtpClient())
                    {
                        await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.Smtport, SecureSocketOptions.StartTls);
                        client.Authenticate(_emailSettings.Username, _emailSettings.Password);
                        await client.SendAsync(message);
                        await client.DisconnectAsync(true);
                    }
                    success = true;
                    emailLog.Status = "Sent";
                    _logger.LogInformation($"Email sent successfully to {recipient} on attempt {attempt}.");
                }
                catch (Exception ex)
                {
                    emailLog.Status = "Failed";
                    emailLog.ErrorMessage = ex.Message;
                    _logger.LogInformation($"Failed to send email to {recipient} on attempt {attempt} ", ex.Message);
                    if (attempt > maxRetries)
                    {
                        _logger.LogError($"Failed to send email after {maxRetries} attempts.");
                    }

                    // slow it down, sometime the app will us up all the retries at once,
                    if (attempt < maxRetries)
                    {
                        await Task.Delay(attempt * 1000);
                    }
                }
                finally
                {
                    _dbContext.Entry(emailLog).State = EntityState.Modified;
                    await _dbContext.SaveChangesAsync();
                }
            }
            if (!success)
            {
                _logger.LogError($"Email sending failed after {maxRetries} attempts to {recipient}.");
                throw new Exception($"Email sending failed after {maxRetries} attempts to {recipient}.");
            }
        }

        private bool isValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch { return false; }
        }

        // personal note, all of these colors and auto completion really make me want to retake comp sci all over again
        // the amount of code that you DONT have to write, becuase the IDE is giving it to you is amazing
        // all I had was a stack of books and a green screen.... ****sigh****** oh well, back to work
        private bool ValidateEmailSettings(EmailSettings emailSettings)
        {
            if (string.IsNullOrEmpty(emailSettings.SmtpServer)) return false;
            if (emailSettings.Smtport <= 0 || emailSettings.Smtport >= 65535) return false;
            if (string.IsNullOrEmpty(emailSettings.Username)) return false;
            if (string.IsNullOrEmpty(emailSettings.Password)) return false;
            if (string.IsNullOrEmpty(emailSettings.SenderName)) return false;
            if (string.IsNullOrEmpty(emailSettings.SenderEmail)) return false;
            if (!isValidEmail(emailSettings.SenderEmail)) return false;
            return true;
        }
    }
}
