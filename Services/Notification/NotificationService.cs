using Ideku.Models;
using Ideku.Models.Entities;
using Ideku.Data.Context;
using Ideku.Services.Email;
using Ideku.Services.WorkflowManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Ideku.Services.Notification
{
    public class NotificationService : INotificationService
    {
        private readonly IEmailService _emailService;
        private readonly AppDbContext _context;
        private readonly IWorkflowManagementService _workflowManagementService;
        private readonly ILogger<NotificationService> _logger;
        private readonly EmailSettings _emailSettings;

        public NotificationService(
            IEmailService emailService, 
            AppDbContext context, 
            IWorkflowManagementService workflowManagementService,
            ILogger<NotificationService> logger,
            IOptions<EmailSettings> emailSettings)
        {
            _emailService = emailService;
            _context = context;
            _workflowManagementService = workflowManagementService;
            _logger = logger;
            _emailSettings = emailSettings.Value;
        }

        public async Task NotifyIdeaSubmitted(Models.Entities.Idea idea, List<User> approvers)
        {
            try
            {
                _logger.LogInformation("Sending idea submitted notification for idea {IdeaId} to {ApproverCount} approvers", 
                    idea.Id, approvers.Count);
                
                if (!approvers.Any())
                {
                    _logger.LogWarning("No approvers provided for idea {IdeaId} at stage {CurrentStage}", 
                        idea.Id, idea.CurrentStage);
                    return;
                }
                
                var emailMessages = new List<EmailMessage>();
                
                foreach (var approver in approvers)
                {
                    var emailMessage = new EmailMessage
                    {
                        To = approver.Employee.EMAIL,
                        Subject = $"New Idea Submission: {idea.IdeaName}",
                        Body = GenerateIdeaSubmittedEmailBody(idea, approver),
                        IsHtml = true
                    };
                    emailMessages.Add(emailMessage);
                }

                if (emailMessages.Any())
                {
                    await _emailService.SendBulkEmailAsync(emailMessages);
                    _logger.LogInformation("Sent idea submission notifications for Idea ID: {IdeaId} to {ApproverCount} approvers", 
                        idea.Id, approvers.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send idea submission notifications for Idea ID: {IdeaId}", idea.Id);
            }
        }

        public async Task NotifyIdeaApproved(Models.Entities.Idea idea, User approver)
        {
            try
            {
                // Notify initiator
                var initiatorEmail = new EmailMessage
                {
                    To = idea.InitiatorUser.Employee.EMAIL,
                    Subject = $"Idea Approved: {idea.IdeaName}",
                    Body = GenerateIdeaApprovedEmailBody(idea, approver),
                    IsHtml = true
                };

                await _emailService.SendEmailAsync(initiatorEmail);
                _logger.LogInformation("Sent idea approval notification for Idea ID: {IdeaId}", idea.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send idea approval notification for Idea ID: {IdeaId}", idea.Id);
            }
        }

        public async Task NotifyIdeaRejected(Models.Entities.Idea idea, User rejector, string reason)
        {
            try
            {
                // Notify initiator
                var initiatorEmail = new EmailMessage
                {
                    To = idea.InitiatorUser.Employee.EMAIL,
                    Subject = $"Idea Rejected: {idea.IdeaName}",
                    Body = GenerateIdeaRejectedEmailBody(idea, rejector, reason),
                    IsHtml = true
                };

                await _emailService.SendEmailAsync(initiatorEmail);
                _logger.LogInformation("Sent idea rejection notification for Idea ID: {IdeaId}", idea.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send idea rejection notification for Idea ID: {IdeaId}", idea.Id);
            }
        }

        public async Task NotifyIdeaCompleted(Models.Entities.Idea idea)
        {
            try
            {
                // Notify initiator and stakeholders
                var emailMessage = new EmailMessage
                {
                    To = idea.InitiatorUser.Employee.EMAIL,
                    Subject = $"Idea Completed: {idea.IdeaName}",
                    Body = GenerateIdeaCompletedEmailBody(idea),
                    IsHtml = true
                };

                await _emailService.SendEmailAsync(emailMessage);
                _logger.LogInformation("Sent idea completion notification for Idea ID: {IdeaId}", idea.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send idea completion notification for Idea ID: {IdeaId}", idea.Id);
            }
        }

        public async Task NotifyMilestoneCreated(Models.Entities.Milestone milestone)
        {
            try
            {
                // Notify idea initiator
                var emailMessage = new EmailMessage
                {
                    To = milestone.Idea.InitiatorUser.Employee.EMAIL,
                    Subject = $"New Milestone Created: {milestone.Idea.IdeaName}",
                    Body = GenerateMilestoneCreatedEmailBody(milestone),
                    IsHtml = true
                };

                await _emailService.SendEmailAsync(emailMessage);
                _logger.LogInformation("Sent milestone creation notification for Milestone ID: {MilestoneId}", milestone.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send milestone creation notification for Milestone ID: {MilestoneId}", milestone.Id);
            }
        }


        private string GenerateIdeaSubmittedEmailBody(Models.Entities.Idea idea, User approver)
        {
            var approvalUrl = $"{_emailSettings.BaseUrl}/Approval/Review/{idea.Id}";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }}
        .container {{ width: 90%; max-width: 1200px; margin: 0 auto; background-color: white; padding: 40px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #1b6ec2, #0077cc); color: white; padding: 25px; border-radius: 8px 8px 0 0; margin: -40px -40px 30px -40px; }}
        .header h1 {{ margin: 0; font-size: 28px; }}
        .content {{ line-height: 1.6; color: #333; }}
        .idea-details {{ background-color: #f8f9fa; padding: 25px; border-radius: 6px; margin: 20px 0; }}
        .action-button {{ display: inline-block; background-color: #1b6ec2; color: white; padding: 15px 30px; text-decoration: none; border-radius: 6px; margin: 20px 0; font-size: 16px; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; font-size: 14px; color: #666; }}
        @media screen and (max-width: 768px) {{ 
            .container {{ max-width: 95%; padding: 20px; }} 
            .header {{ margin: -20px -20px 30px -20px; padding: 20px; }}
            .header h1 {{ font-size: 24px; }}
            .idea-details {{ padding: 20px; }}
            .action-button {{ padding: 12px 24px; font-size: 14px; }}
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>💡 New Idea Requires Approval</h1>
        </div>
        
        <div class='content'>
            <p>Hello {approver.Employee.NAME},</p>
            
            <p>A new idea has been submitted in the Ideku system and requires your approval:</p>
            
            <div class='idea-details'>
                <h3>{idea.IdeaName}</h3>
                <p><strong>Submitted by:</strong> {idea.InitiatorUser?.Name} ({idea.InitiatorUser?.EmployeeId})</p>
                <p><strong>Submission Date:</strong> {idea.SubmittedDate:MMMM dd, yyyy HH:mm}</p>
                <p><strong>Idea ID:</strong> #{idea.IdeaCode}</p>
                <p><strong>Category:</strong> {idea.Category?.CategoryName}</p>
                <p><strong>Estimated Saving:</strong> {idea.SavingCost:C}</p>
            </div>
            
            <p><strong>Idea Description:</strong></p>
            <p style='background-color: #f8f9fa; padding: 15px; border-radius: 4px; margin: 10px 0;'>{idea.IdeaIssueBackground}</p>
            
            <p><strong>Proposed Solution:</strong></p>
            <p style='background-color: #e8f5e8; padding: 15px; border-radius: 4px; margin: 10px 0; border-left: 4px solid #28a745;'>{idea.IdeaSolution}</p>
            
            <p>Please review the idea submission and provide your approval decision:</p>
            
            <a href='{approvalUrl}' class='action-button' style='color: white !important;'>Review & Approve Idea</a>
            
            <p>If you cannot click the button above, copy and paste this URL into your browser:</p>
            <p style='word-break: break-all; background-color: #f8f9fa; padding: 10px; border-radius: 4px;'>{approvalUrl}</p>
            
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

        private string GenerateIdeaApprovedEmailBody(Models.Entities.Idea idea, User approver)
        {
            var ideaUrl = $"{_emailSettings.BaseUrl}/Idea/Details/{idea.Id}";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }}
        .container {{ width: 90%; max-width: 1200px; margin: 0 auto; background-color: white; padding: 40px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #16a085, #27ae60); color: white; padding: 25px; border-radius: 8px 8px 0 0; margin: -40px -40px 30px -40px; }}
        .header h1 {{ margin: 0; font-size: 28px; }}
        .content {{ line-height: 1.6; color: #333; }}
        .idea-details {{ background-color: #f8f9fa; padding: 25px; border-radius: 6px; margin: 20px 0; }}
        .approval-info {{ background-color: #d4edda; padding: 20px; border-radius: 6px; margin: 20px 0; border-left: 4px solid #28a745; }}
        .action-button {{ display: inline-block; background-color: #16a085; color: white; padding: 15px 30px; text-decoration: none; border-radius: 6px; margin: 20px 0; font-size: 16px; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; font-size: 14px; color: #666; }}
        @media screen and (max-width: 768px) {{ 
            .container {{ max-width: 95%; padding: 20px; }} 
            .header {{ margin: -20px -20px 30px -20px; padding: 20px; }}
            .header h1 {{ font-size: 24px; }}
            .idea-details {{ padding: 20px; }}
            .approval-info {{ padding: 15px; }}
            .action-button {{ padding: 12px 24px; font-size: 14px; }}
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 Great News! Your Idea Has Been Approved</h1>
        </div>
        
        <div class='content'>
            <p>Hello {idea.InitiatorUser?.Name},</p>
            
            <p>Congratulations! Your idea has been approved and is moving forward in the review process.</p>
            
            <div class='idea-details'>
                <h3>{idea.IdeaName}</h3>
                <p><strong>Idea ID:</strong> #{idea.IdeaCode}</p>
                <p><strong>Submitted on:</strong> {idea.SubmittedDate:MMMM dd, yyyy HH:mm}</p>
                <p><strong>Saving Cost:</strong> {idea.SavingCost:C}</p>
            </div>
            
            <div class='approval-info'>
                <h4>✅ Approval Details</h4>
                <p><strong>Approved by:</strong> {approver?.Name} ({approver?.Role?.RoleName})</p>
                <p><strong>Status:</strong> {idea.CurrentStatus}</p>
                <p><strong>Approved on:</strong> {DateTime.Now:MMMM dd, yyyy HH:mm}</p>
            </div>
            
            <p>Your idea is now progressing to the next stage of the approval process. You will receive updates as it moves through the system.</p>
            
            <a href='{ideaUrl}' class='action-button' style='color: white !important;'>View Idea Status</a>
            
            <p>If you cannot click the button above, copy and paste this URL into your browser:</p>
            <p style='word-break: break-all; background-color: #f8f9fa; padding: 10px; border-radius: 4px;'>{ideaUrl}</p>
            
            <p>Thank you for your innovative contribution to our organization!</p>
            
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

        private string GenerateIdeaRejectedEmailBody(Models.Entities.Idea idea, User rejector, string reason)
        {
            var ideaUrl = $"{_emailSettings.BaseUrl}/Idea/Details/{idea.Id}";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }}
        .container {{ width: 90%; max-width: 1200px; margin: 0 auto; background-color: white; padding: 40px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #e74c3c, #c0392b); color: white; padding: 25px; border-radius: 8px 8px 0 0; margin: -40px -40px 30px -40px; }}
        .header h1 {{ margin: 0; font-size: 28px; }}
        .content {{ line-height: 1.6; color: #333; }}
        .idea-details {{ background-color: #f8f9fa; padding: 25px; border-radius: 6px; margin: 20px 0; }}
        .feedback-info {{ background-color: #f8d7da; padding: 20px; border-radius: 6px; margin: 20px 0; border-left: 4px solid #dc3545; }}
        .action-button {{ display: inline-block; background-color: #e74c3c; color: white; padding: 15px 30px; text-decoration: none; border-radius: 6px; margin: 20px 0; font-size: 16px; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; font-size: 14px; color: #666; }}
        @media screen and (max-width: 768px) {{ 
            .container {{ max-width: 95%; padding: 20px; }} 
            .header {{ margin: -20px -20px 30px -20px; padding: 20px; }}
            .header h1 {{ font-size: 24px; }}
            .idea-details {{ padding: 20px; }}
            .feedback-info {{ padding: 15px; }}
            .action-button {{ padding: 12px 24px; font-size: 14px; }}
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>❌ Your Idea Has Been Rejected</h1>
        </div>
        
        <div class='content'>
            <p>Hello {idea.InitiatorUser?.Name},</p>
            
            <p>Thank you for submitting your idea. After careful review, your idea has been rejected.</p>
            
            <div class='idea-details'>
                <h3>{idea.IdeaName}</h3>
                <p><strong>Idea ID:</strong> #{idea.IdeaCode}</p>
                <p><strong>Submitted on:</strong> {idea.SubmittedDate:MMMM dd, yyyy HH:mm}</p>
                <p><strong>Current Status:</strong> {idea.CurrentStatus}</p>
            </div>
            
            <div class='feedback-info'>
                <h4>💡 Reviewer Feedback</h4>
                <p><strong>Reviewed by:</strong> {rejector?.Name} ({rejector?.Role?.RoleName})</p>
                <p><strong>Review Date:</strong> {DateTime.Now:MMMM dd, yyyy HH:mm}</p>
                <p><strong>Feedback:</strong></p>
                <div style='background-color: white; padding: 15px; border-radius: 4px; font-style: italic;'>
                    {reason}
                </div>
            </div>
            
            <p>You may revise and resubmit your idea addressing the concerns mentioned above. Please feel free to contact the reviewer for clarification if needed.</p>
            
            <a href='{ideaUrl}' class='action-button' style='color: white !important;'>View Rejection Details</a>
            
            <p>If you cannot click the button above, copy and paste this URL into your browser:</p>
            <p style='word-break: break-all; background-color: #f8f9fa; padding: 10px; border-radius: 4px;'>{ideaUrl}</p>
            
            <p>Thank you for your participation in our innovation process. We encourage you to continue contributing your ideas.</p>
            
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

        private string GenerateIdeaCompletedEmailBody(Models.Entities.Idea idea)
        {
            return $@"
            <h2>Idea Implementation Completed</h2>
            <p>Dear {idea.InitiatorUser.Name},</p>
            <p>Congratulations! Your idea has been successfully implemented:</p>
            <ul>
                <li><strong>Idea:</strong> {idea.IdeaName}</li>
                <li><strong>Code:</strong> {idea.IdeaCode}</li>
                <li><strong>Completed Date:</strong> {idea.CompletedDate:dd/MM/yyyy}</li>
                <li><strong>Final Saving Cost:</strong> {idea.SavingCostValidated:C}</li>
            </ul>
            <p>Thank you for your valuable contribution to our organization!</p>
            <p>Best regards,<br>Ideku System</p>";
        }

        private string GenerateMilestoneCreatedEmailBody(Models.Entities.Milestone milestone)
        {
            return $@"
            <h2>New Milestone Created</h2>
            <p>Dear {milestone.Idea.InitiatorUser.Name},</p>
            <p>A new milestone has been created for your idea:</p>
            <ul>
                <li><strong>Idea:</strong> {milestone.Idea.IdeaName}</li>
                <li><strong>Milestone Status:</strong> {milestone.Status}</li>
                <li><strong>Start Date:</strong> {milestone.StartDate:dd/MM/yyyy}</li>
                <li><strong>End Date:</strong> {milestone.EndDate:dd/MM/yyyy}</li>
                <li><strong>Created by:</strong> {milestone.CreatorName}</li>
            </ul>
            <p><strong>Note:</strong> {milestone.Note}</p>
            <p>Best regards,<br>Ideku System</p>";
        }

        public async Task NotifyWorkstreamLeadersAsync(Models.Entities.Idea idea, List<User> workstreamLeaders)
        {
            try
            {
                if (!workstreamLeaders.Any())
                {
                    _logger.LogInformation("No workstream leaders to notify for idea {IdeaId}", idea.Id);
                    return;
                }

                var emailMessages = new List<EmailMessage>();

                foreach (var workstreamLeader in workstreamLeaders)
                {
                    var emailMessage = new EmailMessage
                    {
                        To = workstreamLeader.Employee.EMAIL,
                        Subject = $"[Ideku] Idea Approved - {idea.IdeaName} (Related to {workstreamLeader.Employee.DivisionNavigation?.NameDivision})",
                        Body = GenerateWorkstreamLeaderNotificationEmailBody(idea, workstreamLeader),
                        IsHtml = true
                    };
                    emailMessages.Add(emailMessage);
                }

                if (emailMessages.Any())
                {
                    await _emailService.SendBulkEmailAsync(emailMessages);
                    _logger.LogInformation("Sent workstream leader notifications for Idea ID: {IdeaId} to {Count} leaders", 
                        idea.Id, emailMessages.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send workstream leader notifications for Idea ID: {IdeaId}", idea.Id);
            }
        }

        private string GenerateWorkstreamLeaderNotificationEmailBody(Models.Entities.Idea idea, User workstreamLeader)
        {
            var ideaUrl = $"https://yourdomain.com/Idea/Details/{idea.Id}"; // Replace with actual base URL
            
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }}
        .container {{ width: 90%; max-width: 1200px; margin: 0 auto; background-color: white; padding: 40px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #17a2b8, #138496); color: white; padding: 25px; border-radius: 8px 8px 0 0; margin: -40px -40px 30px -40px; }}
        .header h1 {{ margin: 0; font-size: 28px; }}
        .content {{ line-height: 1.6; color: #333; }}
        .idea-details {{ background-color: #f8f9fa; padding: 25px; border-radius: 6px; margin: 20px 0; }}
        .action-button {{ display: inline-block; background-color: #17a2b8; color: white; padding: 15px 30px; text-decoration: none; border-radius: 6px; margin: 20px 0; font-size: 16px; }}
        .background-section {{ background-color: #f8f9fa; padding: 20px; border-radius: 6px; margin: 20px 0; border-left: 4px solid #6c757d; }}
        .solution-section {{ background-color: #e8f5e8; padding: 20px; border-radius: 6px; margin: 20px 0; border-left: 4px solid #28a745; }}
        @media screen and (max-width: 768px) {{ 
            .container {{ max-width: 95%; padding: 20px; }} 
            .header {{ margin: -20px -20px 30px -20px; padding: 20px; }}
            .header h1 {{ font-size: 24px; }}
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>💡 Related Idea Approved</h1>
        </div>
        
        <div class='content'>
            <p>Dear {workstreamLeader.Name},</p>
            
            <p>An idea has been approved and your division (<strong>{workstreamLeader.Employee.DivisionNavigation?.NameDivision}</strong>) has been marked as related for potential collaboration.</p>
            
            <div class='idea-details'>
                <h3>{idea.IdeaName}</h3>
                <p><strong>Idea Code:</strong> {idea.IdeaCode}</p>
                <p><strong>Initiator:</strong> {idea.InitiatorUser?.Name}</p>
                <p><strong>Target Division:</strong> {idea.TargetDivision?.NameDivision}</p>
                <p><strong>Category:</strong> {idea.Category?.CategoryName}</p>
                <p><strong>Validated Saving Cost:</strong> {idea.SavingCostValidated:C}</p>
                <p><strong>Current Status:</strong> {idea.CurrentStatus}</p>
            </div>
            
            <div class='background-section'>
                <h4>Idea Description</h4>
                <p>{idea.IdeaIssueBackground}</p>
            </div>
            
            <div class='solution-section'>
                <h4>Idea Solution</h4>
                <p>{idea.IdeaSolution}</p>
            </div>
            
            <p>As the Workstream Leader for <strong>{workstreamLeader.Employee.DivisionNavigation?.NameDivision}</strong>, your division may be involved in the implementation process. Please review the details and prepare for potential collaboration.</p>
            
            <a href='{ideaUrl}' class='action-button' style='color: white !important;'>View Idea Details</a>
            
            <p>If you have questions about this idea or need clarification on your division's involvement, please contact the idea initiator.</p>
            
            <p>Best regards,<br>The Ideku Team</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}