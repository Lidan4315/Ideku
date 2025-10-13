using Ideku.Data.Repositories;
using Ideku.Data.Repositories.WorkflowManagement;
using Ideku.Models.Entities;

namespace Ideku.Services.ChangeWorkflow
{
    /// <summary>
    /// Service implementation for Change Workflow operations
    /// Handles business logic for changing workflow assignments on ideas
    /// </summary>
    public class ChangeWorkflowService : IChangeWorkflowService
    {
        private readonly IIdeaRepository _ideaRepository;
        private readonly IWorkflowManagementRepository _workflowManagementRepository;
        private readonly ILogger<ChangeWorkflowService> _logger;

        public ChangeWorkflowService(
            IIdeaRepository ideaRepository,
            IWorkflowManagementRepository workflowManagementRepository,
            ILogger<ChangeWorkflowService> logger)
        {
            _ideaRepository = ideaRepository;
            _workflowManagementRepository = workflowManagementRepository;
            _logger = logger;
        }

        /// <summary>
        /// Update workflow for an idea with validation
        /// Includes business logic validation before update
        /// </summary>
        public async Task<(bool Success, string Message, string? WorkflowName)> UpdateIdeaWorkflowAsync(
            long ideaId,
            int newWorkflowId,
            string updatedBy)
        {
            try
            {
                // Validate idea exists
                var idea = await _ideaRepository.GetByIdAsync(ideaId);
                if (idea == null)
                {
                    _logger.LogWarning("Attempt to update workflow for non-existent idea {IdeaId} by {User}",
                        ideaId, updatedBy);
                    return (false, "Idea not found", null);
                }

                // Check if idea is deleted
                if (idea.IsDeleted)
                {
                    _logger.LogWarning("Attempt to update workflow for deleted idea {IdeaId} by {User}",
                        ideaId, updatedBy);
                    return (false, "Cannot change workflow for deleted idea", null);
                }

                // Validate workflow exists and is active
                var workflow = await _workflowManagementRepository.GetWorkflowByIdAsync(newWorkflowId);
                if (workflow == null)
                {
                    _logger.LogWarning("Attempt to assign non-existent workflow {WorkflowId} to idea {IdeaId} by {User}",
                        newWorkflowId, ideaId, updatedBy);
                    return (false, "Workflow not found", null);
                }

                if (!workflow.IsActive)
                {
                    _logger.LogWarning("Attempt to assign inactive workflow {WorkflowId} ({WorkflowName}) to idea {IdeaId} by {User}",
                        newWorkflowId, workflow.WorkflowName, ideaId, updatedBy);
                    return (false, $"Workflow '{workflow.WorkflowName}' is not active", null);
                }

                // Check if workflow is already assigned (no need to update)
                if (idea.WorkflowId == newWorkflowId)
                {
                    _logger.LogInformation("Workflow {WorkflowId} is already assigned to idea {IdeaId}",
                        newWorkflowId, ideaId);
                    return (true, $"Idea is already using workflow '{workflow.WorkflowName}'", workflow.WorkflowName);
                }

                // Business rule validation (can be extended)
                var validationResult = await ValidateWorkflowChangeAsync(idea, newWorkflowId, workflow);
                if (!validationResult.Success)
                {
                    return (false, validationResult.Message, null);
                }

                // Store old workflow for logging
                var oldWorkflowId = idea.WorkflowId;

                // Perform update
                idea.WorkflowId = newWorkflowId;
                idea.UpdatedDate = DateTime.Now;

                await _ideaRepository.UpdateAsync(idea);

                _logger.LogInformation(
                    "Workflow updated successfully for idea {IdeaCode} (ID: {IdeaId}) from workflow ID {OldWorkflow} to {NewWorkflow} ({WorkflowName}) by {User}",
                    idea.IdeaCode, ideaId, oldWorkflowId, newWorkflowId, workflow.WorkflowName, updatedBy);

                return (true, $"Workflow successfully changed to '{workflow.WorkflowName}'", workflow.WorkflowName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating workflow for idea {IdeaId} by {User}", ideaId, updatedBy);
                return (false, "An error occurred while updating workflow", null);
            }
        }

        /// <summary>
        /// Validate if workflow change is allowed
        /// Can be extended with custom business rules
        /// </summary>
        private async Task<(bool Success, string Message)> ValidateWorkflowChangeAsync(
            Models.Entities.Idea idea,
            int newWorkflowId,
            Models.Entities.Workflow newWorkflow)
        {
            // Business Rule 1: Cannot change workflow if idea is rejected
            if (idea.IsRejected)
            {
                _logger.LogWarning("Attempt to change workflow for rejected idea {IdeaId}", idea.Id);
                return (false, "Cannot change workflow for rejected ideas");
            }

            // Business Rule 2: Cannot change workflow if idea is completed (optional - uncomment if needed)
            // if (idea.CurrentStatus == "Completed")
            // {
            //     _logger.LogWarning("Attempt to change workflow for completed idea {IdeaId}", idea.Id);
            //     return (false, "Cannot change workflow for completed ideas");
            // }

            // Business Rule 3: Check if new workflow is compatible with idea's category/division/department
            // This can be added based on business requirements
            // Example: Check WorkflowConditions to see if workflow is applicable

            // Add more validation rules here as needed

            return await Task.FromResult((true, "Validation passed"));
        }
    }
}
