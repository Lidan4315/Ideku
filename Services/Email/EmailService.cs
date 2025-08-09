using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Ideku.Models;

namespace Ideku.Services.Email
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(EmailMessage emailMessage)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
                message.To.Add(new MailboxAddress("", emailMessage.To));
                
                if (emailMessage.Cc != null)
                {
                    foreach (var cc in emailMessage.Cc)
                    {
                        message.Cc.Add(new MailboxAddress("", cc));
                    }
                }

                if (emailMessage.Bcc != null)
                {
                    foreach (var bcc in emailMessage.Bcc)
                    {
                        message.Bcc.Add(new MailboxAddress("", bcc));
                    }
                }

                message.Subject = emailMessage.Subject;

                var bodyBuilder = new BodyBuilder();
                if (emailMessage.IsHtml)
                {
                    bodyBuilder.HtmlBody = emailMessage.Body;
                }
                else
                {
                    bodyBuilder.TextBody = emailMessage.Body;
                }
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_emailSettings.SenderEmail, _emailSettings.SenderPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {Email}", emailMessage.To);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", emailMessage.To);
                throw;
            }
        }

        public async Task SendBulkEmailAsync(List<EmailMessage> emailMessages)
        {
            var tasks = emailMessages.Select(SendEmailAsync);
            await Task.WhenAll(tasks);
        }
    }
}