using Ideku.Models.Entities;

namespace Ideku.Data.Repositories.IdeaImplementators
{
    public interface IIdeaImplementatorRepository
    {
        Task<IdeaImplementator> CreateAsync(IdeaImplementator ideaImplementator);
        Task<IdeaImplementator?> GetByIdAsync(long id);
        Task<IEnumerable<IdeaImplementator>> GetByIdeaIdAsync(long ideaId);
        Task<bool> IsUserAssignedToIdeaAsync(long ideaId, long userId);
        Task<bool> HasLeaderAsync(long ideaId);
        Task UpdateAsync(IdeaImplementator ideaImplementator);
        Task<bool> RemoveAsync(long id);

        /// <summary>
        /// Get implementators with User details included for display purposes
        /// </summary>
        Task<IEnumerable<IdeaImplementator>> GetByIdeaIdWithUserAsync(long ideaId);

        /// <summary>
        /// Get count of members (not leaders) for an idea
        /// </summary>
        /// <param name="ideaId">Idea ID</param>
        /// <returns>Count of members assigned to the idea</returns>
        Task<int> GetMemberCountAsync(long ideaId);
    }
}