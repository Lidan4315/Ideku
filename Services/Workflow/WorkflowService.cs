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

        public async Task<IEnumerable<Models.Entities.Idea>> GetPendingApprovalsForUserAsync(string username)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null)
            {
                return new List<Models.Entities.Idea>();
            }

            // For now, this logic is simple: find ideas waiting for the specific user's role.
            // This assumes a simple, linear workflow.
            // A more complex system might check a dedicated "PendingApprover" table.
            
            // We need the IdeaRepository for this. Let's assume it's injected.
            // The actual query logic will depend on the Idea and Role entities.
            // For this example, let's find ideas where the status is "Submitted"
            // and the user's role matches a hypothetical "RequiredRoleForNextStage".
            // This is a placeholder for the real business logic.
            
            // A simple implementation:
            if (user.Role.RoleName == "Superuser")
            {
                // A Superuser can see all pending ideas at any stage
                return await _ideaRepository.GetIdeasByStatusAsync("Waiting Approval S1");
            }
            else if (user.Role.RoleName == "Workstream Leader")
            {
                // A Workstream Leader should approve ideas at stage 0 (just submitted)
                return await _ideaRepository.GetIdeasByStageAndStatusAsync(0, "Waiting Approval S1");
            }

            // Return empty list if the user is not a designated approver in this simple logic
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
            bool canReview = false;
            if (user.Role.RoleName == "Superuser")
            {
                canReview = true; // Superuser can review anything
            }
            else if (user.Role.RoleName == "Workstream Leader" && idea.CurrentStage == 0 && idea.CurrentStatus == "Waiting Approval S1")
            {
                canReview = true;
            }
            // Add other roles and stages here, e.g.:
            // else if (user.Role.RoleName == "Some Other Role" && idea.CurrentStage == 1 && idea.Status == "Pending Stage 2 Approval")
            // {
            //     canReview = true;
            // }

            if (!canReview)
            {
                _logger.LogWarning("User {Username} is not authorized to review idea {IdeaId} at its current stage and status.", username, ideaId);
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

            // TODO: Send notification to the initiator about the rejection.

            await _ideaRepository.UpdateAsync(idea);
        }
    }
}
