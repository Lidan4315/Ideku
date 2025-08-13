using Ideku.Models.Entities;

namespace Ideku.Data.Repositories
{
    public interface ILevelRepository
    {
        Task<IEnumerable<Level>> GetAllAsync();
        Task<Level?> GetByIdAsync(int id);
        Task<Level> AddAsync(Level level);
        Task<Level> UpdateAsync(Level level);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<Role>> GetAllRolesAsync();
        Task<LevelApprover> AddLevelApproverAsync(LevelApprover levelApprover);
        Task<bool> DeleteLevelApproverAsync(int levelApproverId);
    }
}