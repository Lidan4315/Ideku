using Ideku.Models.Entities;

namespace Ideku.Services.Notification
{
    public interface INotificationService
    {
        Task NotifyIdeaSubmitted(Models.Entities.Idea idea, List<User> approvers);
        Task NotifyIdeaApproved(Models.Entities.Idea idea, User approver);
        Task NotifyIdeaRejected(Models.Entities.Idea idea, User rejector, string reason);
        Task NotifyWorkstreamLeadersAsync(Models.Entities.Idea idea, List<User> workstreamLeaders);
        Task NotifyMilestoneCreationRequiredAsync(Models.Entities.Idea idea, List<User> implementators);
        Task NotifyFeedbackSent(Models.Entities.Idea idea, User feedbackSender, string feedbackComment, List<User> workstreamLeaders);
    }
}