using Ideku.Data.Repositories;
using Ideku.Data.Context;
using Ideku.Models;
using Ideku.Services.Email;
using Ideku.Services.WorkflowManagement;
using Ideku.Services.IdeaRelation;
using Ideku.ViewModels.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Ideku.Services.Workflow
{
    public class WorkflowService : IWorkflowService
    {
        private readonly IEmailService _emailService;
        private readonly IUserRepository _userRepository;
        private readonly IIdeaRepository _ideaRepository;
        private readonly IWorkflowRepository _workflowRepository;
        private readonly IWorkflowManagementService _workflowManagementService;
        private readonly IIdeaRelationService _ideaRelationService;
        private readonly ILogger<WorkflowService> _logger;
        private readonly EmailSettings _emailSettings;

        public WorkflowService(
            IEmailService emailService, 
            IUserRepository userRepository, 
            IIdeaRepository ideaRepository, 
            IWorkflowRepository workflowRepository, 
            IWorkflowManagementService workflowManagementService,
            IIdeaRelationService ideaRelationService,
            ILogger<WorkflowService> logger, 
            IOptions<EmailSettings> emailSettings)
        {
            _emailService = emailService;
            _userRepository = userRepository;
            _ideaRepository = ideaRepository;
            _workflowRepository = workflowRepository;
            _workflowManagementService = workflowManagementService;
            _ideaRelationService = ideaRelationService;
            _logger = logger;
            _emailSettings = emailSettings.Value;
        }

        public async Task InitiateWorkflowAsync(Models.Entities.Idea idea)
        {
            _logger.LogInformation("Initiating workflow for idea {IdeaId} - {IdeaName}", idea.Id, idea.IdeaName);
            
            // Get approvers for the first stage of the workflow (stage 1)
            var targetStage = 1; // Idea at S0 needs approval from Stage 1 approvers
            var approvers = await _workflowManagementService.GetApproversForWorkflowStageAsync(idea.WorkflowId, targetStage, idea.ToDivisionId, idea.ToDepartmentId);
            
            _logger.LogInformation("Found {ApproverCount} approvers for idea {IdeaId} at stage {Stage}", 
                approvers.Count(), idea.Id, targetStage);

            if (approvers.Any())
            {
                // Send email to all approvers for this stage
                foreach (var approver in approvers)
                {
                    var emailMessage = new EmailMessage
                    {
                        To = approver.Employee.EMAIL,
                        Subject = $"[Ideku] New Idea Submission Requires Approval - {idea.IdeaName}",
                        Body = GenerateValidationEmailBody(idea, approver, targetStage),
                        IsHtml = true
                    };

                    try
                    {
                        await _emailService.SendEmailAsync(emailMessage);
                        _logger.LogInformation("Workflow initiated for Idea {IdeaId}. Approval email sent to {ApproverEmail} for Stage {Stage}", 
                            idea.Id, approver.Employee.EMAIL, targetStage);
                    }
                    catch (System.Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send workflow initiation email to {ApproverEmail} for Idea {IdeaId}", 
                            approver.Employee.EMAIL, idea.Id);
                    }
                }
            }
            else
            {
                _logger.LogWarning("No approvers found for Idea {IdeaId} at Stage {Stage}. WorkflowId: {WorkflowId}", 
                    idea.Id, targetStage, idea.WorkflowId);
            }
        }

        private string GenerateValidationEmailBody(Models.Entities.Idea idea, Models.Entities.User approver, int targetStage)
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
            
            <p>A new idea has been submitted in the Ideku system and requires your approval at <strong>Stage {targetStage}</strong>:</p>
            
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

        public async Task<IQueryable<Models.Entities.Idea>> GetPendingApprovalsQueryAsync(string username)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null)
            {
                return Enumerable.Empty<Models.Entities.Idea>().AsQueryable();
            }

            // Get base queryable for ideas with all necessary includes
            var baseQuery = _ideaRepository.GetQueryableWithIncludes();

            // Apply role-based filtering using the same logic as GetPendingApprovalsForUserAsync
            if (user.Role.RoleName == "Superuser")
            {
                // Superuser can see all ideas regardless of stage or status
                return baseQuery.Where(idea => 
                    idea.CurrentStatus.StartsWith("Waiting Approval") ||
                    idea.CurrentStatus.StartsWith("Rejected S") ||
                    idea.CurrentStatus == "Approved")
                    .OrderByDescending(idea => idea.SubmittedDate)
                    .ThenByDescending(idea => idea.Id);
            }
            else if (user.Role.RoleName == "Workstream Leader")
            {
                // Workstream Leader can see:
                // - Ideas at stage 0 waiting for S1 approval (their responsibility)
                // - Ideas that were processed at stage 0 (their history)
                return baseQuery.Where(idea => 
                    (idea.CurrentStage == 0 && idea.CurrentStatus == "Waiting Approval S1") || // Current responsibility
                    (idea.CurrentStage >= 1) || // Ideas that have passed their stage
                    (idea.CurrentStatus.StartsWith("Rejected S0")) // Ideas they rejected
                ).OrderByDescending(idea => idea.SubmittedDate)
                .ThenByDescending(idea => idea.Id);
            }

            // Return empty queryable if the user is not a designated approver
            return Enumerable.Empty<Models.Entities.Idea>().AsQueryable();
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

            bool canView = false;
            
            // Superuser can view anything
            if (user.Role.RoleName == "Superuser")
            {
                canView = true;
            }
            else
            {
                // Check if user is an approver for the current stage or has history with this idea
                var targetStage = idea.CurrentStage + 1; // Next stage that needs approval
                
                // Get approvers for the target stage
                var approversForCurrentStage = await _workflowManagementService.GetApproversForWorkflowStageAsync(idea.WorkflowId, targetStage, idea.ToDivisionId, idea.ToDepartmentId);
                
                // User can view if:
                // 1. They are an approver for the current stage that needs approval
                // 2. They have previously acted on this idea (check workflow history)
                canView = approversForCurrentStage.Any(a => a.Id == user.Id);

                if (!canView)
                {
                    // Check if user has history with this idea (previously approved/rejected)
                    var workflowHistory = await _workflowRepository.GetWorkflowHistoryForIdeaAsync(ideaId);
                    canView = workflowHistory.Any(wh => wh.ActorUserId == user.Id);
                }
            }

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

            // Authorization check: User must be authorized approver for the target stage
            var targetStage = idea.CurrentStage + 1; // Stage that needs approval
            var authorizedApprovers = await _workflowManagementService.GetApproversForWorkflowStageAsync(idea.WorkflowId, targetStage, idea.ToDivisionId, idea.ToDepartmentId);
            
            bool isAuthorized = user.Role.RoleName == "Superuser" || 
                               authorizedApprovers.Any(a => a.Id == user.Id);

            if (!isAuthorized)
            {
                _logger.LogWarning("User {Username} with role {RoleName} is not authorized to approve idea {IdeaId} at target stage {TargetStage}", 
                    username, user.Role.RoleName, ideaId, targetStage);
                return;
            }

            var previousStage = idea.CurrentStage;
            var nextStage = idea.CurrentStage + 1;
            
            // Check if we can advance to next stage (validate against MaxStage)
            if (nextStage > idea.MaxStage)
            {
                // Already at final stage - mark as approved/completed
                idea.CurrentStatus = "Approved";
                idea.CompletedDate = DateTime.Now;
                _logger.LogInformation("Idea {IdeaId} completed approval process - reached final stage {MaxStage}", ideaId, idea.MaxStage);
            }
            else
            {
                // Advance to next stage
                idea.CurrentStage = nextStage;
                
                // Set status based on whether this is the final stage or not
                if (nextStage == idea.MaxStage)
                {
                    idea.CurrentStatus = "Approved"; // Final approval
                    idea.CompletedDate = DateTime.Now;
                }
                else
                {
                    idea.CurrentStatus = $"Waiting Approval S{nextStage + 1}";
                }
                
                _logger.LogInformation("Idea {IdeaId} advanced from stage {FromStage} to stage {ToStage}, max stage is {MaxStage}", 
                    ideaId, previousStage, idea.CurrentStage, idea.MaxStage);
            }
            
            idea.UpdatedDate = DateTime.Now;
            
            if (validatedSavingCost.HasValue)
            {
                idea.SavingCostValidated = validatedSavingCost.Value;
            }

            // Add a record to WorkflowHistory
            var workflowHistory = new Models.Entities.WorkflowHistory
            {
                IdeaId = ideaId,
                ActorUserId = user.Id,
                FromStage = previousStage,
                ToStage = nextStage <= idea.MaxStage ? nextStage : (int?)null, // null if completed
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
                    Body = GenerateApprovalEmailBody(idea, user, previousStage, nextStage),
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

            // If idea moved to next stage (not completed), send emails to next stage approvers
            if (nextStage <= idea.MaxStage && idea.CurrentStatus != "Approved")
            {
                var nextStageApprovers = await _workflowManagementService.GetApproversForWorkflowStageAsync(idea.WorkflowId, nextStage + 1, idea.ToDivisionId, idea.ToDepartmentId);
                
                foreach (var nextApprover in nextStageApprovers)
                {
                    try
                    {
                        var nextStageEmailMessage = new EmailMessage
                        {
                            To = nextApprover.Employee.EMAIL,
                            Subject = $"[Ideku] Idea Requires Your Approval - {idea.IdeaName}",
                            Body = GenerateValidationEmailBody(idea, nextApprover, nextStage + 1),
                            IsHtml = true
                        };

                        await _emailService.SendEmailAsync(nextStageEmailMessage);
                        _logger.LogInformation("Next stage approval email sent to {ApproverEmail} for Idea {IdeaId} Stage {Stage}", 
                            nextApprover.Employee.EMAIL, ideaId, nextStage + 1);
                    }
                    catch (System.Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send next stage approval email to {ApproverEmail} for Idea {IdeaId}", 
                            nextApprover.Employee.EMAIL, ideaId);
                    }
                }
            }

            // Log successful approval
            _logger.LogInformation("Idea {IdeaId} successfully approved from S{FromStage} to S{ToStage} by {Username}. Status: {Status}", 
                ideaId, previousStage, nextStage, username, idea.CurrentStatus);
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
batu
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


        private async Task SendApprovalNotificationToInitiatorAsync(Models.Entities.Idea idea)
        {
            try
            {
                var initiatorEmail = idea.InitiatorUser?.Employee?.EMAIL;
                if (string.IsNullOrEmpty(initiatorEmail))
                {
                    _logger.LogWarning("No email found for idea {IdeaId} initiator", idea.Id);
                    return;
                }

                var emailMessage = new EmailMessage
                {
                    To = initiatorEmail,
                    Subject = $"[Ideku] Your Idea Has Been Approved - {idea.IdeaName}",
                    Body = GenerateApprovalNotificationEmail(idea),
                    IsHtml = true
                };

                await _emailService.SendEmailAsync(emailMessage);
                _logger.LogInformation("Approval notification sent to initiator {Email} for idea {IdeaId}", 
                    initiatorEmail, idea.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send approval notification to initiator for idea {IdeaId}", idea.Id);
                // Don't throw - notification failure shouldn't break the approval process
            }
        }

        private string GenerateApprovalNotificationEmail(Models.Entities.Idea idea)
        {
            var ideaUrl = $"{_emailSettings.BaseUrl}/Idea/Details/{idea.Id}";
            
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 8px; }}
        .header {{ background: linear-gradient(135deg, #28a745, #20c997); color: white; padding: 20px; border-radius: 8px; text-align: center; }}
        .content {{ padding: 20px 0; line-height: 1.6; }}
        .action-button {{ display: inline-block; background-color: #28a745; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 Your Idea Has Been Approved!</h1>
        </div>
        <div class='content'>
            <p>Dear {idea.InitiatorUser?.Name},</p>
            
            <p>Great news! Your idea <strong>{idea.IdeaName}</strong> (ID: {idea.IdeaCode}) has been approved and is moving forward in the process.</p>
            
            <p><strong>Current Status:</strong> {idea.CurrentStatus}</p>
            <p><strong>Validated Saving Cost:</strong> {idea.SavingCostValidated:C}</p>
            
            <a href='{ideaUrl}' class='action-button'>View Your Idea</a>
            
            <p>Thank you for your innovation and contribution!</p>
            
            <p>Best regards,<br>The Ideku Team</p>
        </div>
    </div>
</body>
</html>";
        }

        public async Task<WorkflowResult> ProcessApprovalDatabaseAsync(ApprovalProcessDto approvalData)
        {
            try
            {
                _logger.LogInformation("Processing approval database operations for idea {IdeaId} by user {UserId}", 
                    approvalData.IdeaId, approvalData.ApprovedBy);

                var user = await _userRepository.GetByIdAsync(approvalData.ApprovedBy);
                if (user == null)
                {
                    _logger.LogError("User {UserId} not found during approval processing", approvalData.ApprovedBy);
                    return WorkflowResult.Failure("User not found");
                }

                var idea = await _ideaRepository.GetByIdAsync(approvalData.IdeaId);
                if (idea == null)
                {
                    _logger.LogError("Idea {IdeaId} not found", approvalData.IdeaId);
                    return WorkflowResult.Failure("Idea not found");
                }

                var previousStage = idea.CurrentStage;
                var nextStage = idea.CurrentStage + 1;
                
                if (nextStage > idea.MaxStage)
                {
                    idea.CurrentStatus = "Approved";
                    idea.CompletedDate = DateTime.Now;
                }
                else
                {
                    idea.CurrentStage = nextStage;
                    if (nextStage == idea.MaxStage)
                    {
                        idea.CurrentStatus = "Approved";
                        idea.CompletedDate = DateTime.Now;
                    }
                    else
                    {
                        idea.CurrentStatus = $"Waiting Approval S{nextStage + 1}";
                    }
                }
                
                idea.UpdatedDate = DateTime.Now;
                idea.SavingCostValidated = approvalData.ValidatedSavingCost;

                var workflowHistory = new Models.Entities.WorkflowHistory
                {
                    IdeaId = approvalData.IdeaId,
                    ActorUserId = user.Id,
                    FromStage = previousStage,
                    ToStage = nextStage <= idea.MaxStage ? nextStage : (int?)null,
                    Action = "Approved",
                    Comments = approvalData.ApprovalComments,
                    Timestamp = DateTime.Now
                };

                await _workflowRepository.CreateAsync(workflowHistory);

                // Rename files to match new stage
                if (nextStage <= idea.MaxStage && previousStage != nextStage)
                {
                    await RenameFilesToNewStageAsync(approvalData.IdeaId, previousStage, nextStage);
                }

                await _ideaRepository.UpdateAsync(idea);

                if (approvalData.RelatedDivisions?.Any() == true)
                {
                    await _ideaRelationService.UpdateIdeaRelatedDivisionsAsync(
                        approvalData.IdeaId, 
                        approvalData.RelatedDivisions);
                }

                var finalIdea = await _ideaRepository.GetByIdAsync(approvalData.IdeaId);
                
                _logger.LogInformation("Successfully processed database approval for idea {IdeaId}. Status: {Status}", 
                    approvalData.IdeaId, finalIdea?.CurrentStatus);

                return WorkflowResult.Success(
                    approvalData.IdeaId, 
                    finalIdea?.CurrentStage ?? 0, 
                    finalIdea?.CurrentStatus ?? "Unknown",
                    "Idea approved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing approval database operations for idea {IdeaId}", 
                    approvalData.IdeaId);
                return WorkflowResult.Failure($"Error processing approval: {ex.Message}");
            }
        }

        public async Task SendApprovalNotificationsAsync(ApprovalProcessDto approvalData)
        {
            try
            {
                _logger.LogInformation("Sending approval notifications for idea {IdeaId}", approvalData.IdeaId);

                var user = await _userRepository.GetByIdAsync(approvalData.ApprovedBy);
                var idea = await _ideaRepository.GetByIdAsync(approvalData.IdeaId);

                if (user == null || idea == null) return;

                var emailMessage = new EmailMessage
                {
                    To = idea.InitiatorUser.Employee.EMAIL,
                    Subject = $"[Ideku] Your Idea '{idea.IdeaName}' Has Been Approved!",
                    Body = GenerateApprovalEmailBody(idea, user, idea.CurrentStage - 1, idea.CurrentStage),
                    IsHtml = true
                };

                await _emailService.SendEmailAsync(emailMessage);
                _logger.LogInformation("Approval confirmation email sent to initiator {InitiatorEmail} for Idea {IdeaId}", 
                    idea.InitiatorUser.Employee.EMAIL, approvalData.IdeaId);

                if (idea.CurrentStage < idea.MaxStage && idea.CurrentStatus != "Approved")
                {
                    var nextStageApprovers = await _workflowManagementService.GetApproversForWorkflowStageAsync(
                        idea.WorkflowId, idea.CurrentStage + 1, idea.ToDivisionId, idea.ToDepartmentId);
                    
                    foreach (var nextApprover in nextStageApprovers)
                    {
                        var nextStageEmailMessage = new EmailMessage
                        {
                            To = nextApprover.Employee.EMAIL,
                            Subject = $"[Ideku] Idea Requires Your Approval - {idea.IdeaName}",
                            Body = GenerateValidationEmailBody(idea, nextApprover, idea.CurrentStage + 1),
                            IsHtml = true
                        };

                        await _emailService.SendEmailAsync(nextStageEmailMessage);
                        _logger.LogInformation("Next stage approval email sent to {ApproverEmail} for Idea {IdeaId} Stage {Stage}", 
                            nextApprover.Employee.EMAIL, approvalData.IdeaId, idea.CurrentStage + 1);
                    }
                }

                if (approvalData.RelatedDivisions?.Any() == true)
                {
                    await _ideaRelationService.NotifyRelatedDivisionsAsync(idea, approvalData.RelatedDivisions);
                }

                _logger.LogInformation("Successfully sent all approval notifications for idea {IdeaId}", approvalData.IdeaId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send approval notifications for idea {IdeaId}", approvalData.IdeaId);
                throw;
            }
        }

        public async Task ProcessRejectionDatabaseAsync(long ideaId, string username, string reason)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            var idea = await _ideaRepository.GetByIdAsync(ideaId);

            if (user == null || idea == null)
            {
                _logger.LogError("User or Idea not found when processing rejection for IdeaId {IdeaId}", ideaId);
                return;
            }

            idea.IsRejected = true;
            idea.RejectedReason = reason;
            idea.CurrentStatus = $"Rejected S{idea.CurrentStage}";
            idea.UpdatedDate = DateTime.Now;
            idea.CompletedDate = DateTime.Now;

            var workflowHistory = new Models.Entities.WorkflowHistory
            {
                IdeaId = ideaId,
                ActorUserId = user.Id,
                FromStage = idea.CurrentStage,
                ToStage = null,
                Action = "Rejected",
                Comments = reason,
                Timestamp = DateTime.Now
            };

            await _workflowRepository.CreateAsync(workflowHistory);
            await _ideaRepository.UpdateAsync(idea);

            _logger.LogInformation("Successfully processed database rejection for idea {IdeaId}", ideaId);
        }

        public async Task SendRejectionNotificationAsync(long ideaId, string username, string reason)
        {
            try
            {
                var user = await _userRepository.GetByUsernameAsync(username);
                var idea = await _ideaRepository.GetByIdAsync(ideaId);

                if (user == null || idea == null) return;

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send rejection notification email for Idea {IdeaId}", ideaId);
                throw;
            }
        }

        public async Task SaveApprovalFilesAsync(long ideaId, List<IFormFile> files, int stage)
        {
            var idea = await _ideaRepository.GetByIdAsync(ideaId);
            if (idea == null) return;

            var newFilePaths = await HandleFileUploadsAsync(files, idea.IdeaCode, stage, true);
            
            if (newFilePaths.Any())
            {
                var existingFiles = string.IsNullOrEmpty(idea.AttachmentFiles) 
                    ? new List<string>() 
                    : idea.AttachmentFiles.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();

                existingFiles.AddRange(newFilePaths);
                idea.AttachmentFiles = string.Join(";", existingFiles);
                
                await _ideaRepository.UpdateAsync(idea);
                
                _logger.LogInformation("Added {FileCount} approval files to idea {IdeaId} for stage {Stage}", 
                    newFilePaths.Count, ideaId, stage);
            }
        }


        private async Task<List<string>> HandleFileUploadsAsync(List<IFormFile> files, string ideaCode, int stage, bool isApprovalFile = false)
        {
            var filePaths = new List<string>();
            if (files == null || !files.Any()) return filePaths;

            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "ideas");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            // Get existing files count for sequential numbering (ALL files from this idea)
            var ideaPattern = $"{ideaCode}_";
            var existingFiles = Directory.GetFiles(uploadsPath)
                .Where(f => Path.GetFileName(f).StartsWith(ideaPattern))
                .ToList();

            int fileCounter = existingFiles.Count + 1;

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xlsx", ".jpg", ".jpeg", ".png" };
                    var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        throw new InvalidOperationException($"File type {fileExtension} is not allowed");
                    }

                    if (file.Length > 10 * 1024 * 1024)
                    {
                        throw new InvalidOperationException($"File {file.FileName} is too large. Maximum size is 10MB");
                    }

                    var fileName = $"{ideaCode}_S{stage}_{fileCounter:D3}{fileExtension}";
                    var filePath = Path.Combine(uploadsPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    filePaths.Add($"uploads/ideas/{fileName}");
                    fileCounter++;

                    _logger.LogInformation("Uploaded {FileType} file {FileName} for idea {IdeaCode} at stage S{Stage}", 
                        isApprovalFile ? "approval" : "initiator", fileName, ideaCode, stage);
                }
            }

            return filePaths;
        }

        public async Task RenameFilesToNewStageAsync(long ideaId, int fromStage, int toStage)
        {
            var idea = await _ideaRepository.GetByIdAsync(ideaId);
            if (idea == null || string.IsNullOrEmpty(idea.AttachmentFiles)) return;

            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "ideas");
            var filePaths = idea.AttachmentFiles.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
            var updatedPaths = new List<string>();
            var renameOperations = new List<(string oldPath, string newPath)>();

            try
            {
                foreach (var filePath in filePaths)
                {
                    var fileName = Path.GetFileName(filePath);
                    
                    if (fileName.Contains($"_S{fromStage}_"))
                    {
                        var newFileName = fileName.Replace($"_S{fromStage}_", $"_S{toStage}_");
                        var oldFullPath = Path.Combine(uploadsPath, fileName);
                        var newFullPath = Path.Combine(uploadsPath, newFileName);
                        
                        if (File.Exists(oldFullPath))
                        {
                            renameOperations.Add((oldFullPath, newFullPath));
                            updatedPaths.Add($"uploads/ideas/{newFileName}");
                        }
                        else
                        {
                            updatedPaths.Add(filePath);
                        }
                    }
                    else
                    {
                        updatedPaths.Add(filePath);
                    }
                }

                foreach (var (oldPath, newPath) in renameOperations)
                {
                    File.Move(oldPath, newPath);
                    _logger.LogInformation("Renamed file from {OldPath} to {NewPath} for idea {IdeaId}", 
                        Path.GetFileName(oldPath), Path.GetFileName(newPath), ideaId);
                }

                idea.AttachmentFiles = string.Join(";", updatedPaths);
                await _ideaRepository.UpdateAsync(idea);

                _logger.LogInformation("Successfully renamed {Count} files from S{FromStage} to S{ToStage} for idea {IdeaId}", 
                    renameOperations.Count, fromStage, toStage, ideaId);
            }
            catch (Exception ex)
            {
                foreach (var (oldPath, newPath) in renameOperations.Where(op => File.Exists(op.newPath)))
                {
                    try
                    {
                        if (File.Exists(oldPath)) File.Delete(oldPath);
                        File.Move(newPath, oldPath);
                    }
                    catch
                    {
                        _logger.LogError("Failed to rollback file rename: {NewPath} -> {OldPath}", newPath, oldPath);
                    }
                }

                _logger.LogError(ex, "Failed to rename files from S{FromStage} to S{ToStage} for idea {IdeaId}", 
                    fromStage, toStage, ideaId);
                throw;
            }
        }
    }
}
