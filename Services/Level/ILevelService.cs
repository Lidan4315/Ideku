using Ideku.Models.Entities;

namespace Ideku.Services.Level
{
    public interface ILevelService
    {
        Task<IEnumerable<Models.Entities.Level>> GetAllLevelsAsync();
        Task<Models.Entities.Level?> GetLevelByIdAsync(int id);
        Task<Models.Entities.Level> AddLevelAsync(Models.Entities.Level level);
        Task<bool> DeleteLevelAsync(int id);
        Task<IEnumerable<Role>> GetAllRolesAsync();
        Task<LevelApprover> AddLevelApproverAsync(LevelApprover levelApprover);
        Task<bool> DeleteLevelApproverAsync(int levelApproverId);
    }
}