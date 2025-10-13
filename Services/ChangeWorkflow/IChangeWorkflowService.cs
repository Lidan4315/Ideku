using Ideku.Models.Entities;
using Ideku.Models;

namespace Ideku.Services.ChangeWorkflow
{
    public interface IChangeWorkflowService
    {
        Task<(bool Success, string Message, string? WorkflowName)> UpdateIdeaWorkflowAsync(long ideaId, int newWorkflowId, string updatedBy);
    }
}
