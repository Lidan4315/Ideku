using Ideku.Data.Repositories;
using Ideku.Models.Entities;

namespace Ideku.Services.Level
{
    public class LevelService : ILevelService
    {
        private readonly ILevelRepository _levelRepository;

        public LevelService(ILevelRepository levelRepository)
        {
            _levelRepository = levelRepository;
        }

        public async Task<IEnumerable<Models.Entities.Level>> GetAllLevelsAsync()
        {
            return await _levelRepository.GetAllAsync();
        }

        public async Task<Models.Entities.Level?> GetLevelByIdAsync(int id)
        {
            return await _levelRepository.GetByIdAsync(id);
        }

        public async Task<Models.Entities.Level> AddLevelAsync(Models.Entities.Level level)
        {
            return await _levelRepository.AddAsync(level);
        }

        public async Task<bool> DeleteLevelAsync(int id)
        {
            return await _levelRepository.DeleteAsync(id);
        }

        public async Task<IEnumerable<Role>> GetAllRolesAsync()
        {
            return await _levelRepository.GetAllRolesAsync();
        }

        public async Task<LevelApprover> AddLevelApproverAsync(LevelApprover levelApprover)
        {
            return await _levelRepository.AddLevelApproverAsync(levelApprover);
        }

        public async Task<bool> DeleteLevelApproverAsync(int levelApproverId)
        {
            return await _levelRepository.DeleteLevelApproverAsync(levelApproverId);
        }
    }
}