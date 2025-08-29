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
        Task ProcessApprovalAsync(long ideaId, string username, string? comments, decimal? validatedSavingCost);
        Task ProcessRejectionAsync(long ideaId, string username, string reason);
        
        /// <summary>
        /// Process approval with related divisions notification
        /// </summary>
        /// <param name="approvalData">Complete approval data including related divisions</param>
        /// <returns>Result of the approval process</returns>
        Task<WorkflowResult> ProcessApprovalWithRelationsAsync(ApprovalProcessDto approvalData);
    }
}
