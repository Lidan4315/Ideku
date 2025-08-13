using Microsoft.EntityFrameworkCore;
using Ideku.Data.Context;
using Ideku.Models.Entities;

namespace Ideku.Data.Repositories
{
    public class LevelRepository : ILevelRepository
    {
        private readonly AppDbContext _context;

        public LevelRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Level>> GetAllAsync()
        {
            return await _context.Levels
                .OrderBy(l => l.LevelName)
                .ToListAsync();
        }

        public async Task<Level?> GetByIdAsync(int id)
        {
            return await _context.Levels
                .Include(l => l.LevelApprovers)
                    .ThenInclude(la => la.Role)
                .Include(l => l.WorkflowStages)
                .FirstOrDefaultAsync(l => l.Id == id);
        }

        public async Task<Level> AddAsync(Level level)
        {
            _context.Levels.Add(level);
            await _context.SaveChangesAsync();
            return level;
        }

        public async Task<Level> UpdateAsync(Level level)
        {
            _context.Entry(level).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return level;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var level = await _context.Levels.FindAsync(id);
            if (level == null) return false;

            _context.Levels.Remove(level);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Role>> GetAllRolesAsync()
        {
            return await _context.Roles
                .OrderBy(r => r.RoleName)
                .ToListAsync();
        }

        public async Task<LevelApprover> AddLevelApproverAsync(LevelApprover levelApprover)
        {
            _context.LevelApprovers.Add(levelApprover);
            await _context.SaveChangesAsync();
            return levelApprover;
        }

        public async Task<bool> DeleteLevelApproverAsync(int levelApproverId)
        {
            var levelApprover = await _context.LevelApprovers.FindAsync(levelApproverId);
            if (levelApprover == null) return false;

            _context.LevelApprovers.Remove(levelApprover);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}