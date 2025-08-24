using Ideku.Models.Entities;

namespace Ideku.Data.Repositories
{
    public interface IIdeaRepository
    {
        Task<Idea> CreateAsync(Idea idea);
        Task<Idea?> GetByIdAsync(long id);
        Task<IEnumerable<Idea>> GetByInitiatorAsync(long initiatorUserId);
        Task<string> GenerateIdeaCodeAsync();
        Task<bool> IsIdeaCodeExistAsync(string ideaCode);
        string GenerateIdeaCodeFromId(long ideaId);
        Task UpdateIdeaCodeAsync(long ideaId, string ideaCode);
        Task<IEnumerable<Idea>> GetIdeasByStageAndStatusAsync(int stage, string status);
        Task<IEnumerable<Idea>> GetIdeasByStatusAsync(string status);
        Task<IEnumerable<Idea>> GetAllIdeasForApprovalAsync();
        Task UpdateAsync(Idea idea);
        
        /// <summary>
        /// Gets IQueryable with all necessary includes for efficient pagination and filtering
        /// </summary>
        /// <returns>IQueryable of Ideas with related entities included</returns>
        IQueryable<Idea> GetQueryableWithIncludes();

        /// <summary>
        /// Soft delete an idea by setting IsDeleted to true
        /// </summary>
        /// <param name="id">Idea ID to soft delete</param>
        /// <returns>True if successful, false if idea not found</returns>
        Task<bool> SoftDeleteAsync(long id);
    }
}
