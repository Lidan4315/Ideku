using Ideku.Data.Repositories;
using Ideku.Models.Entities;
using Ideku.Services.Notification;
using Ideku.Services.WorkflowManagement;
using IdeaEntity = Ideku.Models.Entities.Idea;

namespace Ideku.Services.BypassStage
{
    public class BypassStageService : IBypassStageService
    {
        private readonly IIdeaRepository _ideaRepository;
        private readonly IWorkflowRepository _workflowRepository;
        private readonly IUserRepository _userRepository;
        private readonly IWorkflowManagementService _workflowManagementService;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<BypassStageService> _logger;

        public BypassStageService(
            IIdeaRepository ideaRepository,
            IWorkflowRepository workflowRepository,
            IUserRepository userRepository,
            IWorkflowManagementService workflowManagementService,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<BypassStageService> logger)
        {
            _ideaRepository = ideaRepository;
            _workflowRepository = workflowRepository;
            _userRepository = userRepository;
            _workflowManagementService = workflowManagementService;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public async Task<(bool Success, string Message, string? NewStatus)> BypassStageAsync(
            long ideaId,
            int targetStage,
            string reason,
            string username)
        {
            try
            {
                // Validate idea exists
                var idea = await _ideaRepository.GetByIdAsync(ideaId);
                if (idea == null)
                {
                    _logger.LogWarning("Attempt to bypass stage for non-existent idea {IdeaId} by {User}",
                        ideaId, username);
                    return (false, "Idea not found", null);
                }

                // Check if idea is deleted
                if (idea.IsDeleted)
                {
                    _logger.LogWarning("Attempt to bypass stage for deleted idea {IdeaId} by {User}",
                        ideaId, username);
                    return (false, "Cannot bypass deleted idea", null);
                }

                // Validate business rules
                var validationResult = await ValidateBypassAsync(idea, targetStage, reason);
                if (!validationResult.Success)
                {
                    return (false, validationResult.Message, null);
                }

                // Get user info
                var user = await _userRepository.GetByUsernameAsync(username);
                if (user == null)
                {
                    _logger.LogWarning("User {Username} not found for bypass operation", username);
                    return (false, "User not found", null);
                }

                // Store old state for logging
                var previousStage = idea.CurrentStage;
                var previousStatus = idea.CurrentStatus;

                // Update idea to target stage
                idea.CurrentStage = targetStage;

                if (targetStage >= idea.MaxStage)
                {
                    idea.CurrentStatus = "Completed";
                    idea.CompletedDate = DateTime.Now;
                }
                else
                {
                    idea.CurrentStatus = $"Waiting Approval S{targetStage + 1}";
                    // Clear CompletedDate if bypassing backward from a completed stage
                    if (targetStage < previousStage && idea.CompletedDate != null)
                    {
                        idea.CompletedDate = null;
                    }
                }

                idea.UpdatedDate = DateTime.Now;

                await _ideaRepository.UpdateAsync(idea);

                // Create WorkflowHistory entry with Action="Bypassed"
                var workflowHistory = new WorkflowHistory
                {
                    IdeaId = ideaId,
                    ActorUserId = user.Id,
                    FromStage = previousStage,
                    ToStage = targetStage >= idea.MaxStage ? (int?)null : targetStage,
                    Action = "Bypassed",
                    Comments = $"Stage bypassed from S{previousStage} to S{targetStage}. Reason: {reason}",
                    Timestamp = DateTime.Now
                };

                await _workflowRepository.CreateAsync(workflowHistory);

                _logger.LogWarning(
                    "Stage bypassed for idea {IdeaCode} (ID: {IdeaId}) from S{FromStage} to S{ToStage} by {User}. Reason: {Reason}",
                    idea.IdeaCode, ideaId, previousStage, targetStage, username, reason);

                // Send notifications in background (fire-and-forget)
                SendBypassNotificationsInBackground(idea, user);

                return (true, $"Stage successfully bypassed to S{targetStage}", idea.CurrentStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bypassing stage for idea {IdeaId} by {User}", ideaId, username);
                return (false, "An error occurred while bypassing stage", null);
            }
        }

        private async Task<(bool Success, string Message)> ValidateBypassAsync(
            IdeaEntity idea,
            int targetStage,
            string reason)
        {
            // Cannot bypass rejected ideas
            if (idea.IsRejected)
            {
                _logger.LogWarning("Attempt to bypass rejected idea {IdeaId}", idea.Id);
                return (false, "Cannot bypass rejected ideas");
            }

            // Cannot bypass already completed ideas
            if (idea.CurrentStatus == "Completed")
            {
                _logger.LogWarning("Attempt to bypass completed idea {IdeaId}", idea.Id);
                return (false, "Idea is already completed");
            }

            // Target stage must be valid
            if (targetStage < 0 || targetStage > idea.MaxStage)
            {
                _logger.LogWarning("Invalid target stage {TargetStage} for idea {IdeaId} (MaxStage: {MaxStage})",
                    targetStage, idea.Id, idea.MaxStage);
                return (false, $"Invalid target stage. Must be between 0 and {idea.MaxStage}");
            }

            // Cannot bypass to the same stage
            if (targetStage == idea.CurrentStage)
            {
                _logger.LogWarning("Attempt to bypass to same stage S{CurrentStage} for idea {IdeaId}",
                    idea.CurrentStage, idea.Id);
                return (false, "Cannot bypass to the same stage");
            }

            // Reason is required
            if (string.IsNullOrWhiteSpace(reason))
            {
                return (false, "Bypass reason is required");
            }

            return await Task.FromResult((true, "Validation passed"));
        }

        private void SendBypassNotificationsInBackground(IdeaEntity idea, User adminUser)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Starting background bypass notification process for idea {IdeaId}", idea.Id);

                    using var scope = _serviceScopeFactory.CreateScope();
                    var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                    var workflowManagementService = scope.ServiceProvider.GetRequiredService<IWorkflowManagementService>();

                    // Notify initiator that stage was bypassed
                    await notificationService.NotifyIdeaApproved(idea, adminUser);
                    _logger.LogInformation("Bypass notification sent to initiator for idea {IdeaId}", idea.Id);

                    // Notify next stage approvers if not completed
                    if (idea.CurrentStatus != "Completed")
                    {
                        // Get approvers for the next stage (CurrentStage + 1)
                        // Because if CurrentStage = 1, the status is "Waiting Approval S2" which needs approvers at WorkflowStage.Stage = 2
                        var nextStageApprovers = await workflowManagementService.GetApproversForWorkflowStageAsync(
                            idea.WorkflowId,
                            idea.CurrentStage + 1,
                            idea.ToDivisionId,
                            idea.ToDepartmentId);

                        if (nextStageApprovers.Any())
                        {
                            await notificationService.NotifyIdeaSubmitted(idea, nextStageApprovers.ToList());
                            _logger.LogInformation("Next stage notification sent for idea {IdeaId} to {ApproverCount} approvers at stage {Stage}",
                                idea.Id, nextStageApprovers.Count(), idea.CurrentStage + 1);
                        }
                    }

                    _logger.LogInformation("Successfully sent all bypass notifications for idea {IdeaId}", idea.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send bypass notifications for idea {IdeaId}", idea.Id);
                }
            });
        }
    }
}
