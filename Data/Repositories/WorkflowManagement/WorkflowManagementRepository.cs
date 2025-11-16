using Microsoft.EntityFrameworkCore;
using Ideku.Data.Context;
using Ideku.Models.Entities;
using Ideku.Helpers;

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
                    .ThenInclude(ws => ws.Approver)
                .Include(w => w.WorkflowConditions)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();
        }

        public async Task<Models.Entities.Workflow?> GetWorkflowByIdAsync(int id)
        {
            return await _context.Workflows
                .Include(w => w.WorkflowStages)
                    .ThenInclude(ws => ws.Approver)
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

        public async Task<IEnumerable<Models.Entities.Approver>> GetAllApproversAsync()
        {
            return await _context.Approvers
                .Where(a => a.IsActive)
                .OrderBy(a => a.ApproverName)
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

        public async Task<IEnumerable<Event>> GetAllEventsAsync()
        {
            return await _context.Events
                .Where(e => e.IsActive)
                .OrderBy(e => e.EventName)
                .ToListAsync();
        }

        public async Task<Models.Entities.Workflow?> GetApplicableWorkflowAsync(int categoryId, string divisionId, string departmentId, long savingCost, int? eventId)
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

        private bool EvaluateNumericCondition(long actualValue, string operatorType, string conditionValue)
        {
            if (!long.TryParse(conditionValue, out long targetValue))
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

        public async Task<IEnumerable<User>> GetApproversForWorkflowStageAsync(int workflowId, int targetStage, string? targetDivisionId, string? targetDepartmentId)
        {
            // Get WorkflowStage for the target stage
            var workflowStage = await _context.WorkflowStages
                .Include(ws => ws.Approver)
                    .ThenInclude(a => a.ApproverRoles)
                        .ThenInclude(la => la.Role)
                            .ThenInclude(r => r.Users)
                                .ThenInclude(u => u.Employee)
                .FirstOrDefaultAsync(ws => ws.WorkflowId == workflowId && ws.Stage == targetStage);

            if (workflowStage == null)
                return new List<User>();

            // Get all users with roles that can approve at this level
            var approverUsers = new List<User>();
            
            foreach (var approverRole in workflowStage.Approver.ApproverRoles)
            {
                // Get all users with the required role and active status
                var usersWithRole = await _context.Users
                    .Include(u => u.Employee)
                    .Include(u => u.Role)
                    .Include(u => u.ActingDivision)
                    .Include(u => u.ActingDepartment)
                    .Where(u => u.RoleId == approverRole.RoleId && u.Employee.EMP_STATUS == "Active")
                    .ToListAsync();

                // Filter by effective location using domain logic (exclusive acting approach)
                var filteredUsers = usersWithRole.Where(u =>
                {
                    // Check division match if specified
                    if (!string.IsNullOrEmpty(targetDivisionId) && LocationHelper.GetEffectiveDivisionId(u) != targetDivisionId)
                        return false;

                    // Check department match if specified
                    if (!string.IsNullOrEmpty(targetDepartmentId) && LocationHelper.GetEffectiveDepartmentId(u) != targetDepartmentId)
                        return false;

                    return true;
                }).ToList();

                approverUsers.AddRange(filteredUsers);
            }

            // Remove duplicates and return
            return approverUsers.DistinctBy(u => u.Id).ToList();
        }

        public async Task<WorkflowStage?> GetWorkflowStageAsync(int workflowId, int stage)
        {
            return await _context.WorkflowStages
                .Include(ws => ws.Approver)
                    .ThenInclude(a => a.ApproverRoles)
                        .ThenInclude(la => la.Role)
                .FirstOrDefaultAsync(ws => ws.WorkflowId == workflowId && ws.Stage == stage);
        }

        public async Task<IEnumerable<(int WorkflowId, int Stage)>> GetWorkflowStagesByRoleIdAsync(int roleId)
        {
            var stages = await _context.WorkflowStages
                .Include(ws => ws.Approver)
                    .ThenInclude(a => a.ApproverRoles)
                .Where(ws => ws.Approver.ApproverRoles.Any(ar => ar.RoleId == roleId))
                .Select(ws => new { ws.WorkflowId, ws.Stage })
                .ToListAsync();

            return stages.Select(s => (s.WorkflowId, s.Stage));
        }

        public async Task<int> GetMaxStageForWorkflowAsync(int workflowId)
        {
            var maxStage = await _context.WorkflowStages
                .Where(ws => ws.WorkflowId == workflowId)
                .MaxAsync(ws => (int?)ws.Stage);

            return maxStage ?? 0;
        }
    }
}