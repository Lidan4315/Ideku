using Microsoft.EntityFrameworkCore;
using Ideku.Data.Context;
using Ideku.Models.Entities;

namespace Ideku.Data.Repositories
{
    public class IdeaRepository : IIdeaRepository
    {
        private readonly AppDbContext _context;

        public IdeaRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Idea> CreateAsync(Idea idea)
        {
            _context.Ideas.Add(idea);
            await _context.SaveChangesAsync();
            return idea;
        }

        public async Task<Idea?> GetByIdAsync(long id)
        {
            return await _context.Ideas
                .Include(i => i.InitiatorUser)
                    .ThenInclude(u => u.Employee)
                .Include(i => i.TargetDivision)
                .Include(i => i.TargetDepartment)
                .Include(i => i.Category)
                .Include(i => i.Event)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<IEnumerable<Idea>> GetByInitiatorAsync(long initiatorUserId)
        {
            return await _context.Ideas
                .Include(i => i.TargetDivision)
                .Include(i => i.TargetDepartment)
                .Include(i => i.Category)
                .Include(i => i.Event)
                .Where(i => i.InitiatorUserId == initiatorUserId)
                .OrderByDescending(i => i.SubmittedDate)
                .ToListAsync();
        }

        public async Task<string> GenerateIdeaCodeAsync()
        {
            // This will be called AFTER the idea is saved to get the actual ID
            // For now, return empty string - we'll update after save
            return string.Empty;
        }

        public string GenerateIdeaCodeFromId(long ideaId)
        {
            // Format: IMS-0000001 (e.g., IMS-0000001, IMS-0000010)
            return $"IMS-{ideaId:D7}";
        }

        public async Task<bool> IsIdeaCodeExistAsync(string ideaCode)
        {
            return await _context.Ideas.AnyAsync(i => i.IdeaCode == ideaCode);
        }

        public async Task UpdateIdeaCodeAsync(long ideaId, string ideaCode)
        {
            var idea = await _context.Ideas.FindAsync(ideaId);
            if (idea != null)
            {
                idea.IdeaCode = ideaCode;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Idea>> GetIdeasByStageAndStatusAsync(int stage, string status)
        {
            return await _context.Ideas
                .Include(i => i.InitiatorUser)
                    .ThenInclude(u => u.Employee)
                .Include(i => i.TargetDivision)
                .Include(i => i.TargetDepartment)
                .Include(i => i.Category)
                .Include(i => i.Event)
                .Where(i => i.CurrentStage == stage && i.CurrentStatus == status)
                .OrderByDescending(i => i.SubmittedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Idea>> GetIdeasByStatusAsync(string status)
        {
            return await _context.Ideas
                .Include(i => i.InitiatorUser)
                    .ThenInclude(u => u.Employee)
                .Include(i => i.TargetDivision)
                .Include(i => i.TargetDepartment)
                .Include(i => i.Category)
                .Include(i => i.Event)
                .Where(i => i.CurrentStatus == status)
                .OrderByDescending(i => i.SubmittedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Idea>> GetAllIdeasForApprovalAsync()
        {
            return await _context.Ideas
                .Include(i => i.InitiatorUser)
                    .ThenInclude(u => u.Employee)
                .Include(i => i.TargetDivision)
                .Include(i => i.TargetDepartment)
                .Include(i => i.Category)
                .Include(i => i.Event)
                .OrderByDescending(i => i.SubmittedDate)
                .ToListAsync();
        }

        public async Task UpdateAsync(Idea idea)
        {
            _context.Ideas.Update(idea);
            await _context.SaveChangesAsync();
        }
    }
}
