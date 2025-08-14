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
        private readonly IIdeaRepository _ideaRepository;
        private readonly IWorkflowRepository _workflowRepository;
        private readonly ILogger<WorkflowService> _logger;
        private readonly EmailSettings _emailSettings;

        public WorkflowService(IEmailService emailService, IUserRepository userRepository, IIdeaRepository ideaRepository, IWorkflowRepository workflowRepository, ILogger<WorkflowService> logger, IOptions<EmailSettings> emailSettings)
        {
            _emailService = emailService;
            _userRepository = userRepository;
            _ideaRepository = ideaRepository;
            _workflowRepository = workflowRepository;
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
                    Subject = $"[Ideku] New Idea Submission Requires Approval - {idea.IdeaName}",
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
            <p>Hello,</p>
            
            <p>A new idea has been submitted in the Ideku system and requires your approval:</p>
            
            <div class='idea-details'>
                <h3>{idea.IdeaName}</h3>
                <p><strong>Submitted by:</strong> {idea.InitiatorUser?.Name} ({idea.InitiatorUser?.EmployeeId})</p>
                <p><strong>Submission Date:</strong> {idea.SubmittedDate:MMMM dd, yyyy HH:mm}</p>
                <p><strong>Idea ID:</strong> #{idea.IdeaCode}</p>
            </div>
            
            <p>Please review the idea submission and provide your approval decision:</p>
            
            <a href='{approvalUrl}' class='action-button' style='color: white !important;'>Review & Approval Idea</a>
            
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

        public async Task<IEnumerable<Models.Entities.Idea>> GetPendingApprovalsForUserAsync(string username)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null)
            {
                return new List<Models.Entities.Idea>();
            }

            // Get all ideas that this user can see based on their role and approval status
            if (user.Role.RoleName == "Superuser")
            {
                // Superuser can see all ideas regardless of stage or status
                return await _ideaRepository.GetAllIdeasForApprovalAsync();
            }
            else if (user.Role.RoleName == "Workstream Leader")
            {
                // Workstream Leader can see:
                // 1. Ideas waiting for their approval (stage 0 -> S1)
                // 2. Ideas they have processed (approved/rejected)
                // 3. Ideas that have moved beyond their stage
                
                var allIdeas = await _ideaRepository.GetAllIdeasForApprovalAsync();
                
                // Filter to show ideas relevant to Workstream Leader:
                // - Ideas at stage 0 waiting for S1 approval (their responsibility)
                // - Ideas that were processed at stage 0 (their history)
                return allIdeas.Where(idea => 
                    (idea.CurrentStage == 0 && idea.CurrentStatus == "Waiting Approval S1") || // Current responsibility
                    (idea.CurrentStage >= 1) || // Ideas that have passed their stage
                    (idea.CurrentStatus.StartsWith("Rejected S0")) // Ideas they rejected
                ).ToList();
            }

            // Return empty list if the user is not a designated approver
            return new List<Models.Entities.Idea>();
        }

        public async Task<Models.Entities.Idea> GetIdeaForReview(int ideaId, string username)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null)
            {
                _logger.LogWarning("No user found with username {Username} when trying to review idea {IdeaId}", username, ideaId);
                return null;
            }

            var idea = await _ideaRepository.GetByIdAsync(ideaId);
            if (idea == null)
            {
                _logger.LogWarning("No idea found with ID {IdeaId}", ideaId);
                return null;
            }

            // Simple authorization logic:
            // Does the user's role match the required role for the idea's current stage?
            // This is a placeholder for more complex workflow logic.
            bool canView = false;
            if (user.Role.RoleName == "Superuser")
            {
                canView = true; // Superuser can view anything
            }
            else if (user.Role.RoleName == "Workstream Leader")
            {
                // Workstream Leader can view:
                // 1. Ideas they can currently approve (stage 0, waiting S1)
                // 2. Ideas that have moved beyond their stage (history/context)
                // 3. Ideas they have rejected
                canView = (idea.CurrentStage == 0 && idea.CurrentStatus == "Waiting Approval S1") || // Can review
                         (idea.CurrentStage >= 1) || // Can view history
                         (idea.CurrentStatus.StartsWith("Rejected S0")); // Can view their rejections
            }
            // Add other roles and stages here as needed

            if (!canView)
            {
                _logger.LogWarning("User {Username} is not authorized to view idea {IdeaId} at its current stage and status.", username, ideaId);
                return null;
            }

            return idea;
        }

        public async Task ProcessApprovalAsync(long ideaId, string username, string? comments, decimal? validatedSavingCost)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            var idea = await _ideaRepository.GetByIdAsync(ideaId);

            if (user == null || idea == null)
            {
                _logger.LogError("User or Idea not found when processing approval for IdeaId {IdeaId}", ideaId);
                return;
            }

            // Authorization check: Only Workstream Leader or Superuser can approve S0->S1
            if (idea.CurrentStage == 0 && user.Role.RoleName != "Workstream Leader" && user.Role.RoleName != "Superuser")
            {
                _logger.LogWarning("User {Username} with role {RoleName} is not authorized to approve idea {IdeaId} at stage {Stage}", 
                    username, user.Role.RoleName, ideaId, idea.CurrentStage);
                return;
            }

            var previousStage = idea.CurrentStage;
            
            // Update from S0 to S1
            idea.CurrentStage = 1;
            idea.CurrentStatus = "Waiting Approval S2";
            idea.UpdatedDate = DateTime.Now;
            
            if (validatedSavingCost.HasValue)
            {
                idea.SavingCostVaidated = validatedSavingCost.Value;
            }

            // Add a record to WorkflowHistory
            var workflowHistory = new Models.Entities.WorkflowHistory
            {
                IdeaId = ideaId,
                ActorUserId = user.Id,
                FromStage = previousStage,
                ToStage = idea.CurrentStage,
                Action = "Approved",
                Comments = comments,
                Timestamp = DateTime.Now
            };

            // Save to WorkflowRepository and update idea
            await _workflowRepository.CreateAsync(workflowHistory);
            await _ideaRepository.UpdateAsync(idea);

            // Send approval confirmation email to initiator
            try
            {
                var emailMessage = new EmailMessage
                {
                    To = idea.InitiatorUser.Employee.EMAIL,
                    Subject = $"[Ideku] Your Idea '{idea.IdeaName}' Has Been Approved!",
                    Body = GenerateApprovalEmailBody(idea, user, previousStage, idea.CurrentStage),
                    IsHtml = true
                };

                await _emailService.SendEmailAsync(emailMessage);
                _logger.LogInformation("Approval confirmation email sent to initiator {InitiatorEmail} for Idea {IdeaId}", 
                    idea.InitiatorUser.Employee.EMAIL, ideaId);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Failed to send approval confirmation email for Idea {IdeaId}", ideaId);
            }

            // Log successful approval
            _logger.LogInformation("Idea {IdeaId} successfully approved from S{FromStage} to S{ToStage} by {Username}", 
                ideaId, previousStage, idea.CurrentStage, username);
        }

        public async Task ProcessRejectionAsync(long ideaId, string username, string reason)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            var idea = await _ideaRepository.GetByIdAsync(ideaId);

            if (user == null || idea == null)
            {
                _logger.LogError("User or Idea not found when processing rejection for IdeaId {IdeaId}", ideaId);
                return; // Or throw an exception
            }

            idea.IsRejected = true;
            idea.RejectedReason = reason;
            idea.CurrentStatus = $"Rejected S{idea.CurrentStage}";
            idea.UpdatedDate = DateTime.Now;
            idea.CompletedDate = DateTime.Now; // Rejection also completes the workflow for this idea.

            // Add a record to WorkflowHistory for rejection
            var workflowHistory = new Models.Entities.WorkflowHistory
            {
                IdeaId = ideaId,
                ActorUserId = user.Id,
                FromStage = idea.CurrentStage,
                ToStage = null, // Rejection doesn't move to next stage
                Action = "Rejected",
                Comments = reason,
                Timestamp = DateTime.Now
            };

            // Save to WorkflowRepository
            await _workflowRepository.CreateAsync(workflowHistory);

            // Send rejection notification email to initiator
            try
            {
                var emailMessage = new EmailMessage
                {
                    To = idea.InitiatorUser.Employee.EMAIL,
                    Subject = $"[Ideku] Your Idea '{idea.IdeaName}' Has Been Rejected",
                    Body = GenerateRejectionEmailBody(idea, user, reason),
                    IsHtml = true
                };

                await _emailService.SendEmailAsync(emailMessage);
                _logger.LogInformation("Rejection notification email sent to initiator {InitiatorEmail} for Idea {IdeaId}", 
                    idea.InitiatorUser.Employee.EMAIL, ideaId);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Failed to send rejection notification email for Idea {IdeaId}", ideaId);
            }

            await _ideaRepository.UpdateAsync(idea);
        }

        private string GenerateApprovalEmailBody(Models.Entities.Idea idea, Models.Entities.User approver, int fromStage, int toStage)
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
                <p><strong>Stage Progress:</strong> S{fromStage} → S{toStage}</p>
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

        private string GenerateRejectionEmailBody(Models.Entities.Idea idea, Models.Entities.User reviewer, string reason)
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
            <p>ta {idea.InitiatorUser?.Name},</p>
            
            <p>Thank you for submitting your idea. After careful review, your idea has been rejected.</p>
            
            <div class='idea-details'>
                <h3>{idea.IdeaName}</h3>
                <p><strong>Idea ID:</strong> #{idea.IdeaCode}</p>
                <p><strong>Submitted on:</strong> {idea.SubmittedDate:MMMM dd, yyyy HH:mm}</p>
                <p><strong>Current Status:</strong> {idea.CurrentStatus}</p>
            </div>
            
            <div class='feedback-info'>
                <h4>💡 Reviewer Feedback</h4>
                <p><strong>Reviewed by:</strong> {reviewer?.Name} ({reviewer?.Role?.RoleName})</p>
                <p><strong>Review Date:</strong> {DateTime.Now:MMMM dd, yyyy HH:mm}</p>
                <p><strong>Feedback:</strong></p>
                <div style='background-color: white; padding: 15px; border-radius: 4px; font-style: italic;'>
                    {reason}
                </div>
            </div>
            
            <p>You can view the details and feedback by clicking the link below.</p>
            
            <a href='{ideaUrl}' class='action-button' style='color: white !important;'>View Rejection Details</a>
            
            <p>If you cannot click the button above, copy and paste this URL into your browser:</p>
            <p style='word-break: break-all; background-color: #f8f9fa; padding: 10px; border-radius: 4px;'>{ideaUrl}</p>
            
            <p><small>Thank you for your participation in our innovation process.</small></p>
            
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
