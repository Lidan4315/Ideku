using Ideku.ViewModels.DTOs;

namespace Ideku.Services.Notification
{
    /// Service untuk coordinate background notification jobs.
    public interface INotificationCoordinatorService
    {
        void NotifyNextStageApproversInBackground(long ideaId);
        void NotifyApprovalInBackground(ApprovalProcessDto approvalData);
        void NotifyRejectionInBackground(long ideaId, string username, string reason);
        void NotifyFeedbackInBackground(long ideaId, string username, string feedbackComment);
    }
}
