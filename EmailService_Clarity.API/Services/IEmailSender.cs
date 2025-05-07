namespace EmailService_Clarity25.API.Services
{
    using System.Threading.Tasks;
    public interface IEmailSender
    {
        Task SendEmailAsync(string recipeint, string subject, string body);
    }
}
