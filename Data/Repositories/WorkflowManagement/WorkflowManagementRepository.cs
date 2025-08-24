using Microsoft.EntityFrameworkCore;
using Ideku.Data.Context;
using Ideku.Models.Entities;

namespace Ideku.Data.Repositories.WorkflowManagement
{
    public class WorkflowManagementRepository : IWorkflowManagementRepository
    {
        private readonly AppDbContext _context;

        public WorkflowManagementRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Models.Entities.Workflow>> GetAllWorkflowsAsync()
        {
            return await _context.Workflows
                .Include(w => w.WorkflowStages)
                    .ThenInclude(ws => ws.Level)
                .Include(w => w.WorkflowConditions)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();
        }

        public async Task<Models.Entities.Workflow?> GetWorkflowByIdAsync(int id)
        {
            return await _context.Workflows
                .Include(w => w.WorkflowStages)
                    .ThenInclude(ws => ws.Level)
                .Include(w => w.WorkflowConditions)
                .FirstOrDefaultAsync(w => w.Id == id);
        }

        public async Task<Models.Entities.Workflow> AddWorkflowAsync(Models.Entities.Workflow workflow)
        {
            _context.Workflows.Add(workflow);
            await _context.SaveChangesAsync();
            return workflow;
        }

        public async Task<bool> UpdateWorkflowAsync(Models.Entities.Workflow workflow)
        {
            _context.Workflows.Update(workflow);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> DeleteWorkflowAsync(int id)
        {
            var workflow = await _context.Workflows.FindAsync(id);
            if (workflow == null) return false;

            _context.Workflows.Remove(workflow);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<WorkflowStage> AddWorkflowStageAsync(WorkflowStage workflowStage)
        {
            _context.WorkflowStages.Add(workflowStage);
            await _context.SaveChangesAsync();
            return workflowStage;
        }

        public async Task<bool> DeleteWorkflowStageAsync(int workflowStageId)
        {
            var workflowStage = await _context.WorkflowStages.FindAsync(workflowStageId);
            if (workflowStage == null) return false;

            _context.WorkflowStages.Remove(workflowStage);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<WorkflowCondition> AddWorkflowConditionAsync(WorkflowCondition workflowCondition)
        {
            _context.WorkflowConditions.Add(workflowCondition);
            await _context.SaveChangesAsync();
            return workflowCondition;
        }

        public async Task<bool> DeleteWorkflowConditionAsync(int workflowConditionId)
        {
            var workflowCondition = await _context.WorkflowConditions.FindAsync(workflowConditionId);
            if (workflowCondition == null) return false;

            _context.WorkflowConditions.Remove(workflowCondition);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<IEnumerable<Models.Entities.Level>> GetAllLevelsAsync()
        {
            return await _context.Levels
                .Where(l => l.IsActive)
                .OrderBy(l => l.LevelName)
                .ToListAsync();
        }

        public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
        {
            return await _context.Categories
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
        }

        public async Task<IEnumerable<Division>> GetAllDivisionsAsync()
        {
            return await _context.Divisions
                .OrderBy(d => d.NameDivision)
                .ToListAsync();
        }

        public async Task<IEnumerable<Department>> GetAllDepartmentsAsync()
        {
            return await _context.Departments
                .Include(d => d.Division)
                .OrderBy(d => d.NameDepartment)
                .ToListAsync();
        }

        public async Task<Models.Entities.Workflow?> GetApplicableWorkflowAsync(int categoryId, string divisionId, string departmentId, decimal savingCost, int? eventId)
        {
            // Get all active workflows with their conditions, ordered by priority (highest first)
            var workflows = await _context.Workflows
                .Where(w => w.IsActive)
                .Include(w => w.WorkflowConditions.Where(wc => wc.IsActive))
                .OrderByDescending(w => w.Priority)
                .ToListAsync();

            foreach (var workflow in workflows)
            {
                // If workflow has no conditions, it's a default/fallback workflow
                if (!workflow.WorkflowConditions.Any())
                {
                    continue; // Skip for now, will be used as fallback
                }

                // Check if ALL conditions are met for this workflow
                bool allConditionsMet = true;

                foreach (var condition in workflow.WorkflowConditions)
                {
                    bool conditionMet = condition.ConditionType switch
                    {
                        "SAVING_COST" => EvaluateNumericCondition(savingCost, condition.Operator, condition.ConditionValue),
                        "CATEGORY" => EvaluateListCondition(categoryId.ToString(), condition.Operator, condition.ConditionValue),
                        "DIVISION" => EvaluateListCondition(divisionId, condition.Operator, condition.ConditionValue),
                        "DEPARTMENT" => EvaluateListCondition(departmentId, condition.Operator, condition.ConditionValue),
                        "EVENT" => EvaluateListCondition(eventId?.ToString() ?? "", condition.Operator, condition.ConditionValue),
                        _ => false
                    };

                    if (!conditionMet)
                    {
                        allConditionsMet = false;
                        break;
                    }
                }

                // If all conditions are met, return this workflow
                if (allConditionsMet)
                {
                    return workflow;
                }
            }

            // If no specific workflow found, return the highest priority workflow without conditions (default)
            return workflows.FirstOrDefault(w => !w.WorkflowConditions.Any());
        }

        private bool EvaluateNumericCondition(decimal actualValue, string operatorType, string conditionValue)
        {
            if (!decimal.TryParse(conditionValue, out decimal targetValue))
                return false;

            return operatorType switch
            {
                ">=" => actualValue >= targetValue,
                "<=" => actualValue <= targetValue,
                "=" => actualValue == targetValue,
                "!=" => actualValue != targetValue,
                ">" => actualValue > targetValue,
                "<" => actualValue < targetValue,
                _ => false
            };
        }

        private bool EvaluateListCondition(string actualValue, string operatorType, string conditionValue)
        {
            if (string.IsNullOrEmpty(actualValue))
                return false;

            var values = conditionValue.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                     .Select(v => v.Trim())
                                     .ToList();

            return operatorType switch
            {
                "IN" => values.Contains(actualValue, StringComparer.OrdinalIgnoreCase),
                "NOT_IN" => !values.Contains(actualValue, StringComparer.OrdinalIgnoreCase),
                "=" => values.Contains(actualValue, StringComparer.OrdinalIgnoreCase),
                "!=" => !values.Contains(actualValue, StringComparer.OrdinalIgnoreCase),
                _ => false
            };
        }
    }
}