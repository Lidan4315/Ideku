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
    }
}