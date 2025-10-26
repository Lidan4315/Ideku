using Microsoft.EntityFrameworkCore;
using Ideku.Data.Context;
using Ideku.Models.Entities;

namespace Ideku.Data.Repositories
{
    public class IdeaMonitoringRepository : IIdeaMonitoringRepository
    {
        private readonly AppDbContext _context;

        public IdeaMonitoringRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IdeaMonitoring> CreateAsync(IdeaMonitoring monitoring)
        {
            _context.IdeaMonitorings.Add(monitoring);
            await _context.SaveChangesAsync();
            return monitoring;
        }

        public async Task<IdeaMonitoring?> GetByIdAsync(long id)
        {
            return await _context.IdeaMonitorings
                .Include(im => im.Idea)
                    .ThenInclude(i => i.InitiatorUser)
                        .ThenInclude(u => u.Employee)
                .Include(im => im.Idea.Category)
                .Include(im => im.Idea.TargetDivision)
                .Include(im => im.Idea.TargetDepartment)
                .FirstOrDefaultAsync(im => im.Id == id);
        }

        public async Task<IEnumerable<IdeaMonitoring>> GetByIdeaIdAsync(long ideaId)
        {
            return await _context.IdeaMonitorings
                .Where(im => im.IdeaId == ideaId)
                .OrderByDescending(im => im.MonthFrom)
                .ToListAsync();
        }

        public async Task UpdateAsync(IdeaMonitoring monitoring)
        {
            monitoring.UpdatedAt = DateTime.Now;
            _context.IdeaMonitorings.Update(monitoring);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var monitoring = await _context.IdeaMonitorings.FindAsync(id);
            if (monitoring == null)
            {
                return false;
            }

            _context.IdeaMonitorings.Remove(monitoring);
            await _context.SaveChangesAsync();
            return true;
        }

        public IQueryable<IdeaMonitoring> GetQueryableWithIncludes()
        {
            return _context.IdeaMonitorings
                .Include(im => im.Idea)
                    .ThenInclude(i => i.InitiatorUser)
                        .ThenInclude(u => u.Employee)
                .Include(im => im.Idea.Category)
                .Include(im => im.Idea.TargetDivision)
                .Include(im => im.Idea.TargetDepartment);
        }
    }
}
