using Ideku.Models.Entities;
using Ideku.ViewModels.WorkflowManagement;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Ideku.Services.WorkflowManagement
{
    public interface IWorkflowManagementService
    {
        Task<IEnumerable<Models.Entities.Workflow>> GetAllWorkflowsAsync();
        Task<Models.Entities.Workflow> GetWorkflowByIdAsync(int id);
        Task<Models.Entities.Workflow> AddWorkflowAsync(Models.Entities.Workflow workflow);
        Task<bool> UpdateWorkflowAsync(Models.Entities.Workflow workflow);
        Task<bool> UpdateWorkflowAsync(EditWorkflowViewModel editModel);
        Task<bool> DeleteWorkflowAsync(int id);

        // WorkflowStage Operations
        Task<WorkflowStage> AddWorkflowStageAsync(WorkflowStage workflowStage);
        Task<bool> DeleteWorkflowStageAsync(int workflowStageId);

        // WorkflowCondition Operations
        Task<WorkflowCondition> AddWorkflowConditionAsync(WorkflowCondition workflowCondition);
        Task<bool> DeleteWorkflowConditionAsync(int workflowConditionId);

        //Helper methods
        Task<IEnumerable<Models.Entities.Approver>> GetAllApproversAsync();
        Task<IEnumerable<Category>> GetAllCategoriesAsync();
        Task<IEnumerable<Division>> GetAllDivisionsAsync();
        Task<IEnumerable<Department>> GetAllDepartmentsAsync();
        Task<IEnumerable<Event>> GetAllEventsAsync();

        // Workflow Selection
        Task<Models.Entities.Workflow?> GetApplicableWorkflowAsync(int categoryId, string divisionId, string departmentId, decimal savingCost, int? eventId);

        // Stage & Approver Management
        Task<IEnumerable<User>> GetApproversForWorkflowStageAsync(int workflowId, int targetStage, string? targetDivisionId, string? targetDepartmentId);
        Task<WorkflowStage?> GetWorkflowStageAsync(int workflowId, int stage);
    }
}