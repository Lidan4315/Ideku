using Microsoft.EntityFrameworkCore;
using Ideku.Data.Context;
using Ideku.Models.Entities;

namespace Ideku.Data.Repositories.IdeaImplementators
{
    public class IdeaImplementatorRepository : IIdeaImplementatorRepository
    {
        private readonly AppDbContext _context;

        public IdeaImplementatorRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IdeaImplementator> CreateAsync(IdeaImplementator ideaImplementator)
        {
            _context.IdeaImplementators.Add(ideaImplementator);
            await _context.SaveChangesAsync();
            return ideaImplementator;
        }

        public async Task<IdeaImplementator?> GetByIdAsync(long id)
        {
            return await _context.IdeaImplementators
                .Include(ii => ii.User)
                    .ThenInclude(u => u.Employee)
                .Include(ii => ii.Idea)
                .FirstOrDefaultAsync(ii => ii.Id == id);
        }

        public async Task<IEnumerable<IdeaImplementator>> GetByIdeaIdAsync(long ideaId)
        {
            return await _context.IdeaImplementators
                .Where(ii => ii.IdeaId == ideaId)
                .ToListAsync();
        }

        public async Task<bool> IsUserAssignedToIdeaAsync(long ideaId, long userId)
        {
            return await _context.IdeaImplementators
                .AnyAsync(ii => ii.IdeaId == ideaId && ii.UserId == userId);
        }

        public async Task<bool> HasLeaderAsync(long ideaId)
        {
            return await _context.IdeaImplementators
                .AnyAsync(ii => ii.IdeaId == ideaId && ii.Role == "Leader");
        }

        public async Task UpdateAsync(IdeaImplementator ideaImplementator)
        {
            ideaImplementator.UpdatedAt = DateTime.Now;
            _context.IdeaImplementators.Update(ideaImplementator);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> RemoveAsync(long id)
        {
            var implementator = await _context.IdeaImplementators.FindAsync(id);
            if (implementator == null)
                return false;

            _context.IdeaImplementators.Remove(implementator);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<IdeaImplementator>> GetByIdeaIdWithUserAsync(long ideaId)
        {
            return await _context.IdeaImplementators
                .Include(ii => ii.User)
                    .ThenInclude(u => u.Employee)
                        .ThenInclude(e => e.DivisionNavigation)
                .Include(ii => ii.User)
                    .ThenInclude(u => u.Employee)
                        .ThenInclude(e => e.DepartmentNavigation)
                .Include(ii => ii.User)
                    .ThenInclude(u => u.Role)
                .Where(ii => ii.IdeaId == ideaId)
                .OrderBy(ii => ii.Role) // Leader first, then Members
                .ThenBy(ii => ii.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetMemberCountAsync(long ideaId)
        {
            return await _context.IdeaImplementators
                .Where(ii => ii.IdeaId == ideaId && ii.Role == "Member")
                .CountAsync();
        }

        public async Task<bool> IsUserLeaderOfIdeaAsync(long userId, long ideaId)
        {
            return await _context.IdeaImplementators
                .AnyAsync(ii => ii.IdeaId == ideaId && ii.UserId == userId && ii.Role == "Leader");
        }
    }
}