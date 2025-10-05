using Ideku.Data.Context;
using Ideku.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ideku.Data.Repositories
{
    public class MilestoneRepository : IMilestoneRepository
    {
        private readonly AppDbContext _context;

        public MilestoneRepository(AppDbContext context)
        {
            _context = context;
        }

        public IQueryable<Idea> GetIdeasWithMilestoneEligibility()
        {
            return _context.Ideas
                .Include(i => i.InitiatorUser)
                    .ThenInclude(u => u.Employee)
                .Include(i => i.TargetDivision)
                .Include(i => i.TargetDepartment)
                .Include(i => i.Category)
                .Include(i => i.Event)
                .Include(i => i.Workflow)
                .Include(i => i.Milestones)
                    .ThenInclude(m => m.MilestonePICs)
                        .ThenInclude(mp => mp.User)
                .Include(i => i.IdeaImplementators)
                    .ThenInclude(ii => ii.User)
                .Where(i => !i.IsDeleted && i.CurrentStage >= 2)
                .OrderByDescending(i => i.SubmittedDate);
        }

        public async Task<Milestone?> GetMilestoneByIdAsync(long id)
        {
            return await _context.Milestones
                .Include(m => m.Idea)
                    .ThenInclude(i => i.InitiatorUser)
                .Include(m => m.MilestonePICs)
                    .ThenInclude(mp => mp.User)
                        .ThenInclude(u => u.Employee)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<IEnumerable<Milestone>> GetMilestonesByIdeaIdAsync(long ideaId)
        {
            return await _context.Milestones
                .Include(m => m.MilestonePICs)
                    .ThenInclude(mp => mp.User)
                        .ThenInclude(u => u.Employee)
                .Where(m => m.IdeaId == ideaId)
                .OrderBy(m => m.StartDate)
                .ToListAsync();
        }

        public async Task<Milestone> CreateMilestoneAsync(Milestone milestone)
        {
            milestone.CreatedAt = DateTime.Now;
            _context.Milestones.Add(milestone);
            await _context.SaveChangesAsync();
            return milestone;
        }

        public async Task<Milestone> UpdateMilestoneAsync(Milestone milestone)
        {
            milestone.UpdatedAt = DateTime.Now;
            _context.Milestones.Update(milestone);
            await _context.SaveChangesAsync();
            return milestone;
        }

        public async Task<bool> DeleteMilestoneAsync(long id)
        {
            var milestone = await _context.Milestones.FindAsync(id);
            if (milestone == null) return false;

            _context.Milestones.Remove(milestone);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsIdeaMilestoneEligibleAsync(long ideaId)
        {
            var idea = await _context.Ideas
                .Where(i => i.Id == ideaId && !i.IsDeleted)
                .Select(i => new { i.CurrentStage })
                .FirstOrDefaultAsync();

            return idea != null && idea.CurrentStage >= 2;
        }

        public async Task<IEnumerable<MilestonePIC>> GetMilestonePICsAsync(long milestoneId)
        {
            return await _context.MilestonePICs
                .Include(mp => mp.User)
                    .ThenInclude(u => u.Employee)
                .Where(mp => mp.MilestoneId == milestoneId)
                .ToListAsync();
        }

        public async Task<IEnumerable<MilestonePIC>> AddMilestonePICsAsync(IEnumerable<MilestonePIC> milestonePICs)
        {
            var pics = milestonePICs.ToList();
            foreach (var pic in pics)
            {
                pic.CreatedAt = DateTime.UtcNow;
            }

            _context.MilestonePICs.AddRange(pics);
            await _context.SaveChangesAsync();
            return pics;
        }

        public async Task<IEnumerable<MilestonePIC>> UpdateMilestonePICsAsync(long milestoneId, IEnumerable<MilestonePIC> newPICs)
        {
            // Remove existing PICs
            var existingPICs = await _context.MilestonePICs
                .Where(mp => mp.MilestoneId == milestoneId)
                .ToListAsync();

            _context.MilestonePICs.RemoveRange(existingPICs);

            // Add new PICs
            var pics = newPICs.ToList();
            foreach (var pic in pics)
            {
                pic.MilestoneId = milestoneId;
                pic.CreatedAt = DateTime.UtcNow;
            }

            _context.MilestonePICs.AddRange(pics);
            await _context.SaveChangesAsync();
            return pics;
        }
    }
}