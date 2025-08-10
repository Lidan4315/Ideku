using Microsoft.EntityFrameworkCore;
using Ideku.Data.Context;
using Ideku.Models.Entities;

namespace Ideku.Data.Repositories
{
    public class WorkflowRepository : IWorkflowRepository
    {
        private readonly AppDbContext _context;

        public WorkflowRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<WorkflowHistory> CreateAsync(WorkflowHistory workflowHistory)
        {
            _context.WorkflowHistories.Add(workflowHistory);
            await _context.SaveChangesAsync();
            return workflowHistory;
        }

        public async Task<IEnumerable<WorkflowHistory>> GetByIdeaIdAsync(long ideaId)
        {
            return await _context.WorkflowHistories
                .Include(wh => wh.ActorUser)
                .Where(wh => wh.IdeaId == ideaId)
                .OrderByDescending(wh => wh.Timestamp)
                .ToListAsync();
        }
    }
}