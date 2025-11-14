using System.Threading.Tasks;
using Ideku.ViewModels.DTOs;

namespace Ideku.Services.Workflow
{
    public interface IWorkflowService
    {
        Task InitiateWorkflowAsync(Models.Entities.Idea idea);
        Task<IEnumerable<Models.Entities.Idea>> GetPendingApprovalsForUserAsync(string username);
        
        /// <summary>
        /// Gets IQueryable for pending approvals to support efficient pagination and filtering
        /// </summary>
        /// <param name="username">Username of the approver</param>
        /// <returns>IQueryable of Ideas that can be further filtered and paginated</returns>
        Task<IQueryable<Models.Entities.Idea>> GetPendingApprovalsQueryAsync(string username);
        
        Task<Models.Entities.Idea> GetIdeaForReview(int ideaId, string username);
        Task ProcessApprovalAsync(long ideaId, string username, string? comments, long? validatedSavingCost);

        /// <summary>
        /// Process approval database operations only (no email sending)
        /// </summary>
        /// <param name="approvalData">Complete approval data including related divisions</param>
        /// <returns>Result of the database operations</returns>
        Task<WorkflowResult> ProcessApprovalDatabaseAsync(ApprovalProcessDto approvalData);

        /// <summary>
        /// Send approval notification emails only
        /// </summary>
        /// <param name="approvalData">Approval data for email context</param>
        /// <returns>Task</returns>
        Task SendApprovalNotificationsAsync(ApprovalProcessDto approvalData);

        /// <summary>
        /// Process rejection database operations only (no email sending)
        /// </summary>
        /// <param name="ideaId">ID of the idea to reject</param>
        /// <param name="username">Username of the user rejecting</param>
        /// <param name="reason">Rejection reason</param>
        /// <returns>Task</returns>
        Task ProcessRejectionDatabaseAsync(long ideaId, string username, string reason);

        /// <summary>
        /// Send rejection notification email only
        /// </summary>
        /// <param name="ideaId">ID of the rejected idea</param>
        /// <param name="username">Username of the user who rejected</param>
        /// <param name="reason">Rejection reason</param>
        /// <returns>Task</returns>
        Task SendRejectionNotificationAsync(long ideaId, string username, string reason);

        /// <summary>
        /// Save approval files with proper naming convention
        /// </summary>
        /// <param name="ideaId">ID of the idea</param>
        /// <param name="files">Files to upload</param>
        /// <param name="stage">Current approval stage</param>
        /// <returns>Task</returns>
        Task SaveApprovalFilesAsync(long ideaId, List<IFormFile> files, int stage);

        /// <summary>
        /// Rename existing files to match new stage
        /// </summary>
        /// <param name="ideaId">ID of the idea</param>
        /// <param name="fromStage">Old stage number</param>
        /// <param name="toStage">New stage number</param>
        /// <returns>Task</returns>
        Task RenameFilesToNewStageAsync(long ideaId, int fromStage, int toStage);

        /// <summary>
        /// Submit idea to next stage approval (used when milestone is ready for S3 approval)
        /// </summary>
        /// <param name="ideaId">ID of the idea</param>
        /// <param name="username">Username of the user submitting</param>
        /// <returns>Workflow result indicating success or failure</returns>
        Task<WorkflowResult> SubmitForNextStageApprovalAsync(long ideaId, string username);

        /// <summary>
        /// Get workflow history for an idea
        /// </summary>
        /// <param name="ideaId">ID of the idea</param>
        /// <returns>List of workflow history entries</returns>
        Task<IEnumerable<Models.Entities.WorkflowHistory>> GetWorkflowHistoryByIdeaIdAsync(long ideaId);

        /// <summary>
        /// Get approvers for the next stage of an idea
        /// </summary>
        /// <param name="idea">The idea to get approvers for</param>
        /// <returns>List of users who can approve the next stage</returns>
        Task<List<Models.Entities.User>> GetApproversForNextStageAsync(Models.Entities.Idea idea);

        /// <summary>
        /// Process feedback database operations only (no email sending)
        /// Creates WorkflowHistory record with Action="Feedback", idea stage remains unchanged
        /// </summary>
        /// <param name="ideaId">ID of the idea to provide feedback on</param>
        /// <param name="username">Username of the user providing feedback</param>
        /// <param name="feedbackComment">Feedback comment text</param>
        /// <returns>Task</returns>
        Task ProcessFeedbackAsync(long ideaId, string username, string feedbackComment);

        /// <summary>
        /// Send feedback notification emails only
        /// Sends to initiator and workstream leaders in the idea's division/department
        /// </summary>
        /// <param name="ideaId">ID of the idea</param>
        /// <param name="username">Username of the user who provided feedback</param>
        /// <param name="feedbackComment">Feedback comment text</param>
        /// <returns>Task</returns>
        Task SendFeedbackNotificationsAsync(long ideaId, string username, string feedbackComment);
    }
}
