using System.Threading.Tasks;
using Ideku.ViewModels.DTOs;

namespace Ideku.Services.Workflow
{
    public interface IWorkflowService
    {
        Task InitiateWorkflowAsync(Models.Entities.Idea idea);
        Task<IEnumerable<Models.Entities.Idea>> GetPendingApprovalsForUserAsync(string username);

        /// Gets IQueryable for pending approvals to support efficient pagination and filtering
        Task<IQueryable<Models.Entities.Idea>> GetPendingApprovalsQueryAsync(string username);
        
        Task<Models.Entities.Idea> GetIdeaForReview(int ideaId, string username);
        Task ProcessApprovalAsync(long ideaId, string username, string? comments, long? validatedSavingCost);

        /// Process approval database operations only (no email sending)
        Task<WorkflowResult> ProcessApprovalDatabaseAsync(ApprovalProcessDto approvalData);

        /// Send approval notification emails only
        Task SendApprovalNotificationsAsync(ApprovalProcessDto approvalData);

        /// Process rejection database operations only (no email sending)
        Task ProcessRejectionDatabaseAsync(long ideaId, string username, string reason);

        /// Send rejection notification email only
        Task SendRejectionNotificationAsync(long ideaId, string username, string reason);

        /// Save approval files with proper naming convention
        Task SaveApprovalFilesAsync(long ideaId, List<IFormFile> files, int stage);

        /// Submit idea to next stage approval (used when milestone is ready for S3 approval)
        Task<WorkflowResult> SubmitForNextStageApprovalAsync(long ideaId, string username);

        /// Get workflow history for an idea
        Task<IEnumerable<Models.Entities.WorkflowHistory>> GetWorkflowHistoryByIdeaIdAsync(long ideaId);

        /// Get approvers for the next stage of an idea
        Task<List<Models.Entities.User>> GetApproversForNextStageAsync(Models.Entities.Idea idea);

        /// Process feedback database operations only (no email sending)
        /// Creates WorkflowHistory record with Action="Feedback", idea stage remains unchanged
        Task ProcessFeedbackAsync(long ideaId, string username, string feedbackComment);

        /// Send feedback notification emails only
        /// Sends to initiator and workstream leaders in the idea's division/department
        Task SendFeedbackNotificationsAsync(long ideaId, string username, string feedbackComment);
    }
}
