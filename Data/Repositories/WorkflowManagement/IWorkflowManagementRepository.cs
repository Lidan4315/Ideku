using Ideku.Models.Entities;

namespace Ideku.Data.Repositories.WorkflowManagement
{
    public interface IWorkflowManagementRepository
    {
        // Workflow Operations
        Task<IEnumerable<Models.Entities.Workflow>> GetAllWorkflowsAsync();
        Task<Models.Entities.Workflow> GetWorkflowByIdAsync(int id);
        Task<Models.Entities.Workflow> AddWorkflowAsync(Models.Entities.Workflow workflow);
        Task<bool> UpdateWorkflowAsync(Models.Entities.Workflow workflow);
        Task<bool> DeleteWorkflowAsync(int id);

        // WorkflowStage Operations
        Task<WorkflowStage> AddWorkflowStageAsync(WorkflowStage workflowStage);
        Task<bool> DeleteWorkflowStageAsync(int workflowStageId);

        // WorkflowCondition Operations
        Task<WorkflowCondition> AddWorkflowConditionAsync(WorkflowCondition workflowCondition);
        Task<bool> DeleteWorkflowConditionAsync(int workflowConditionId);

        // Workflow Selection
        Task<Models.Entities.Workflow?> GetApplicableWorkflowAsync(int categoryId, string divisionId, string departmentId, decimal savingCost, int? eventId);

        // Helper Methods for Dropdowns
        Task<IEnumerable<Models.Entities.Level>> GetAllLevelsAsync();
        Task<IEnumerable<Category>> GetAllCategoriesAsync();
        Task<IEnumerable<Division>> GetAllDivisionsAsync();
        Task<IEnumerable<Department>> GetAllDepartmentsAsync();
    }
}