using Ideku.Models.Entities;

namespace Ideku.Data.Repositories
{
    public interface IIdeaMonitoringRepository
    {
        Task<IdeaMonitoring> CreateAsync(IdeaMonitoring monitoring);
        Task<IdeaMonitoring?> GetByIdAsync(long id);
        Task<IEnumerable<IdeaMonitoring>> GetByIdeaIdAsync(long ideaId);
        Task UpdateAsync(IdeaMonitoring monitoring);
        Task<bool> DeleteAsync(long id);
        IQueryable<IdeaMonitoring> GetQueryableWithIncludes();
    }
}
