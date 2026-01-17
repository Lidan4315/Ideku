using Ideku.Services.BackgroundServices;
using Ideku.Services.Workflow;
using Ideku.Data.Repositories;
using Ideku.ViewModels.DTOs;
using Microsoft.Extensions.Logging;

namespace Ideku.Services.Notification
{
    /// Coordinates background notification jobs.
    public class NotificationCoordinatorService : INotificationCoordinatorService
    {
        private readonly IBackgroundJobService _backgroundJobService;
        private readonly ILogger<NotificationCoordinatorService> _logger;

        public NotificationCoordinatorService(
            IBackgroundJobService backgroundJobService,
            ILogger<NotificationCoordinatorService> logger)
        {
            _backgroundJobService = backgroundJobService;
            _logger = logger;
        }

        public void NotifyNextStageApproversInBackground(long ideaId)
        {
            _backgroundJobService.ExecuteInBackground(
                $"NotifyApprovers-{ideaId}",
                async (serviceProvider) =>
                {
                    // Get fresh scoped services dari background scope
                    var workflowService = serviceProvider.GetRequiredService<IWorkflowService>();
                    var ideaRepository = serviceProvider.GetRequiredService<IIdeaRepository>();

                    // Load idea dengan fresh DbContext
                    var idea = await ideaRepository.GetByIdAsync(ideaId);
                    if (idea == null)
                    {
                        throw new InvalidOperationException($"Idea {ideaId} not found for notification");
                    }
                    await workflowService.InitiateWorkflowAsync(idea);
                }
            );
        }

        public void NotifyApprovalInBackground(ApprovalProcessDto approvalData)
        {
            _backgroundJobService.ExecuteInBackground(
                $"NotifyApproval-{approvalData.IdeaId}",
                async (serviceProvider) =>
                {
                    var workflowService = serviceProvider.GetRequiredService<IWorkflowService>();
                    await workflowService.SendApprovalNotificationsAsync(approvalData);
                }
            );
        }

        public void NotifyRejectionInBackground(long ideaId, string username, string reason)
        {
            _backgroundJobService.ExecuteInBackground(
                $"NotifyRejection-{ideaId}",
                async (serviceProvider) =>
                {
                    var workflowService = serviceProvider.GetRequiredService<IWorkflowService>();
                    await workflowService.SendRejectionNotificationAsync(ideaId, username, reason);
                }
            );
        }

        public void NotifyFeedbackInBackground(long ideaId, string username, string feedbackComment)
        {
            _backgroundJobService.ExecuteInBackground(
                $"NotifyFeedback-{ideaId}",
                async (serviceProvider) =>
                {
                    var workflowService = serviceProvider.GetRequiredService<IWorkflowService>();
                    await workflowService.SendFeedbackNotificationsAsync(ideaId, username, feedbackComment);
                }
            );
        }
    }
}
