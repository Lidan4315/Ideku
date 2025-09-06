using Ideku.Data.Repositories.WorkflowManagement;
using Ideku.Models.Entities;
using Ideku.ViewModels.WorkflowManagement;

namespace Ideku.Services.WorkflowManagement
{
    public class WorkflowManagementService : IWorkflowManagementService
    {
        private readonly IWorkflowManagementRepository _workflowRepository;

        public WorkflowManagementService(IWorkflowManagementRepository workflowRepository)
        {
            _workflowRepository = workflowRepository;
        }

        // Workflow operations - Service layer biasanya hanya meneruskan ke repository
        // tapi bisa ditambah validasi bisnis di sini jika diperlukan
        public async Task<IEnumerable<Models.Entities.Workflow>> GetAllWorkflowsAsync()
        {
            return await _workflowRepository.GetAllWorkflowsAsync();
        }

        public async Task<Models.Entities.Workflow?> GetWorkflowByIdAsync(int id)
        {
            return await _workflowRepository.GetWorkflowByIdAsync(id);
        }

        public async Task<Models.Entities.Workflow> AddWorkflowAsync(Models.Entities.Workflow workflow)
        {
            // Validasi: Workflow name harus unique
            var existingWorkflows = await _workflowRepository.GetAllWorkflowsAsync();
            if (existingWorkflows.Any(w => w.WorkflowName.ToLower() == workflow.WorkflowName.ToLower()))
            {
                throw new InvalidOperationException("Workflow with this name already exists.");
            }

            return await _workflowRepository.AddWorkflowAsync(workflow);
        }

        public async Task<bool> UpdateWorkflowAsync(Models.Entities.Workflow workflow)
        {
            // Validasi: Workflow name harus unique (kecuali untuk workflow yang sama)
            var existingWorkflows = await _workflowRepository.GetAllWorkflowsAsync();
            if (existingWorkflows.Any(w => w.WorkflowName.ToLower() == workflow.WorkflowName.ToLower() && w.Id != workflow.Id))
            {
                throw new InvalidOperationException("Workflow with this name already exists.");
            }

            workflow.UpdatedAt = DateTime.Now;
            return await _workflowRepository.UpdateWorkflowAsync(workflow);
        }

        public async Task<bool> UpdateWorkflowAsync(EditWorkflowViewModel editModel)
        {
            // Get existing workflow
            var existingWorkflow = await _workflowRepository.GetWorkflowByIdAsync(editModel.Id);
            if (existingWorkflow == null)
            {
                throw new InvalidOperationException("Workflow not found.");
            }

            // Validasi: Workflow name harus unique (kecuali untuk workflow yang sama)
            var existingWorkflows = await _workflowRepository.GetAllWorkflowsAsync();
            if (existingWorkflows.Any(w => w.WorkflowName.ToLower() == editModel.WorkflowName.ToLower() && w.Id != editModel.Id))
            {
                throw new InvalidOperationException("Workflow with this name already exists.");
            }

            // Update properties
            existingWorkflow.WorkflowName = editModel.WorkflowName;
            existingWorkflow.Desc = editModel.Desc;
            existingWorkflow.Priority = editModel.Priority;
            existingWorkflow.IsActive = editModel.IsActive;
            existingWorkflow.UpdatedAt = DateTime.Now;

            return await _workflowRepository.UpdateWorkflowAsync(existingWorkflow);
        }

        public async Task<bool> DeleteWorkflowAsync(int id)
        {
            return await _workflowRepository.DeleteWorkflowAsync(id);
        }

        // WorkflowStage operations
        public async Task<WorkflowStage> AddWorkflowStageAsync(WorkflowStage workflowStage)
        {
            // Validasi: Stage number harus unique dalam satu workflow
            var workflow = await _workflowRepository.GetWorkflowByIdAsync(workflowStage.WorkflowId);
            if (workflow == null)
            {
                throw new InvalidOperationException("Workflow not found.");
            }

            if (workflow.WorkflowStages.Any(ws => ws.Stage == workflowStage.Stage))
            {
                throw new InvalidOperationException("Stage number already exists in this workflow.");
            }

            return await _workflowRepository.AddWorkflowStageAsync(workflowStage);
        }

        public async Task<bool> DeleteWorkflowStageAsync(int workflowStageId)
        {
            return await _workflowRepository.DeleteWorkflowStageAsync(workflowStageId);
        }

        // WorkflowCondition operations
        public async Task<WorkflowCondition> AddWorkflowConditionAsync(WorkflowCondition workflowCondition)
        {
            return await _workflowRepository.AddWorkflowConditionAsync(workflowCondition);
        }

        public async Task<bool> DeleteWorkflowConditionAsync(int workflowConditionId)
        {
            return await _workflowRepository.DeleteWorkflowConditionAsync(workflowConditionId);
        }

        // Helper methods - untuk dropdown/select options
        public async Task<IEnumerable<Models.Entities.Approver>> GetAllApproversAsync()
        {
            return await _workflowRepository.GetAllApproversAsync();
        }

        public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
        {
            return await _workflowRepository.GetAllCategoriesAsync();
        }

        public async Task<IEnumerable<Division>> GetAllDivisionsAsync()
        {
            return await _workflowRepository.GetAllDivisionsAsync();
        }

        public async Task<IEnumerable<Department>> GetAllDepartmentsAsync()
        {
            return await _workflowRepository.GetAllDepartmentsAsync();
        }

        public async Task<IEnumerable<Event>> GetAllEventsAsync()
        {
            return await _workflowRepository.GetAllEventsAsync();
        }

        public async Task<Models.Entities.Workflow?> GetApplicableWorkflowAsync(int categoryId, string divisionId, string departmentId, decimal savingCost, int? eventId)
        {
            return await _workflowRepository.GetApplicableWorkflowAsync(categoryId, divisionId, departmentId, savingCost, eventId);
        }

        public async Task<IEnumerable<User>> GetApproversForWorkflowStageAsync(int workflowId, int targetStage, string? targetDivisionId, string? targetDepartmentId)
        {
            return await _workflowRepository.GetApproversForWorkflowStageAsync(workflowId, targetStage, targetDivisionId, targetDepartmentId);
        }

        public async Task<WorkflowStage?> GetWorkflowStageAsync(int workflowId, int stage)
        {
            return await _workflowRepository.GetWorkflowStageAsync(workflowId, stage);
        }
    }
}