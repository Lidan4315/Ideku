using Ideku.Models.Entities;

namespace Ideku.Services.Notification
{
    public interface INotificationService
    {
        Task NotifyIdeaSubmitted(Idea idea);
        Task NotifyIdeaApproved(Idea idea, User approver);
        Task NotifyIdeaRejected(Idea idea, User rejector, string reason);
        Task NotifyIdeaCompleted(Idea idea);
        Task NotifyMilestoneCreated(Milestone milestone);
    }
}