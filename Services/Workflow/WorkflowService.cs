using Ideku.Data.Repositories;
using Ideku.Models;
using Ideku.Services.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace Ideku.Services.Workflow
{
    public class WorkflowService : IWorkflowService
    {
        private readonly IEmailService _emailService;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<WorkflowService> _logger;
        private readonly EmailSettings _emailSettings;

        public WorkflowService(IEmailService emailService, IUserRepository userRepository, ILogger<WorkflowService> logger, IOptions<EmailSettings> emailSettings)
        {
            _emailService = emailService;
            _userRepository = userRepository;
            _logger = logger;
            _emailSettings = emailSettings.Value;
        }

        public async Task InitiateWorkflowAsync(Models.Entities.Idea idea)
        {
            // Find the first approver, which should be the Workstream Leader.
            var firstApprover = await _userRepository.GetUserByRoleAsync("Workstream Leader");

            if (firstApprover != null)
            {
                var emailMessage = new EmailMessage
                {
                    To = firstApprover.Employee.EMAIL,
                    Subject = $"[Ideku] New Idea Submission Requires Validation - {idea.IdeaName}",
                    Body = GenerateValidationEmailBody(idea),
                    IsHtml = true
                };

                try
                {
                    await _emailService.SendEmailAsync(emailMessage);
                    _logger.LogInformation("Workflow initiated for Idea {IdeaId}. Approval email sent to {ApproverEmail}", idea.Id, firstApprover.Employee.EMAIL);
                }
                catch (System.Exception ex)
                {
                    _logger.LogError(ex, "Failed to send workflow initiation email for Idea {IdeaId}", idea.Id);
                }
            }
            else
            {
                _logger.LogWarning("Could not find an approver with the 'Workstream Leader' role to initiate workflow for Idea {IdeaId}", idea.Id);
            }
        }

        private string GenerateValidationEmailBody(Models.Entities.Idea idea)
        {
            var validationUrl = $"{_emailSettings.BaseUrl}/Validation/Review/{idea.Id}"; // Assuming a similar URL structure

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #1b6ec2, #0077cc); color: white; padding: 20px; border-radius: 8px 8px 0 0; margin: -30px -30px 30px -30px; }}
        .header h1 {{ margin: 0; font-size: 24px; }}
        .content {{ line-height: 1.6; color: #333; }}
        .idea-details {{ background-color: #f8f9fa; padding: 20px; border-radius: 6px; margin: 20px 0; }}
        .action-button {{ display: inline-block; background-color: #1b6ec2; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; margin: 20px 0; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; font-size: 14px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>💡 New Idea Requires Validation</h1>
        </div>
        
        <div class='content'>
            <p>Hello,</p>
            
            <p>A new idea has been submitted in the Ideku system and requires your validation:</p>
            
            <div class='idea-details'>
                <h3>{idea.IdeaName}</h3>
                <p><strong>Submitted by:</strong> {idea.InitiatorUser?.Name} ({idea.InitiatorUser?.EmployeeId})</p>
                <p><strong>Submission Date:</strong> {idea.SubmittedDate:MMMM dd, yyyy HH:mm}</p>
                <p><strong>Idea ID:</strong> #{idea.IdeaCode}</p>
            </div>
            
            <p>Please review the idea submission and provide your validation decision:</p>
            
            <a href='{validationUrl}' class='action-button' style='color: white !important;'>Review & Validate Idea</a>
            
            <p>If you cannot click the button above, copy and paste this URL into your browser:</p>
            <p style='word-break: break-all; background-color: #f8f9fa; padding: 10px; border-radius: 4px;'>{validationUrl}</p>
            
            <p>Thank you for your time and contribution to the innovation process.</p>
            
            <p>Best regards,<br>
            The Ideku Team</p>
        </div>
        
        <div class='footer'>
            <p>This is an automated message from the Ideku Idea Management System. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
