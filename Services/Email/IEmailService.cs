using Ideku.Models;

namespace Ideku.Services.Email
{
    public interface IEmailService
    {
        Task SendEmailAsync(EmailMessage emailMessage);
        Task SendBulkEmailAsync(List<EmailMessage> emailMessages);
    }
}