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
    }
}