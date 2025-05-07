using EmailService_Clarity25.API.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class EmailController : ControllerBase
{
    private readonly IEmailSender _emailSender;
    private readonly ILogger<EmailController> _logger;

    public EmailController(IEmailSender emailSender, ILogger<EmailController> logger)
    {
        _emailSender = emailSender;
        _logger = logger;
    }

    [HttpPost("send-test-email")]
    public async Task<IActionResult> SendTestEmail([FromQuery] string recipient)
    {
        if (string.IsNullOrEmpty(recipient))
        {
            return BadRequest("Recipient email address is required.");
        }

        try
        {
            string subject = "Test Email from James' Email Clarity App";
            string body = $"This is a test of the Clarity email service project sent to {recipient} at {DateTime.UtcNow}";
            await _emailSender.SendEmailAsync(recipient, subject, body);
            _logger.LogInformation($"Test email sent successfully to {recipient}.");
            return Ok(new { message = "Email sent successfully!" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send test email to {recipient}.");
            return StatusCode(500, new { message = $"Failed to send email: {ex.Message}" });
        }
    }
}