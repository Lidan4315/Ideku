using Ideku.Models.Entities;

namespace Ideku.Data.Repositories
{
    public interface IWorkflowRepository
    {
        Task<WorkflowHistory> CreateAsync(WorkflowHistory workflowHistory);
        Task<IEnumerable<WorkflowHistory>> GetByIdeaIdAsync(long ideaId);
    }
}