using System.Threading.Tasks;

namespace Ideku.Services.Workflow
{
    public interface IWorkflowService
    {
        Task InitiateWorkflowAsync(Models.Entities.Idea idea);
        Task<IEnumerable<Models.Entities.Idea>> GetPendingApprovalsForUserAsync(string username);
        Task<Models.Entities.Idea> GetIdeaForReview(int ideaId, string username);
        Task ProcessApprovalAsync(long ideaId, string username, string? comments, decimal? validatedSavingCost);
        Task ProcessRejectionAsync(long ideaId, string username, string reason);
    }
}
