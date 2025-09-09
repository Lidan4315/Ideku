using Ideku.ViewModels;
using Ideku.Models.Entities;

namespace Ideku.Services.Idea
{
    public interface IIdeaService
    {
        Task<CreateIdeaViewModel> PrepareCreateViewModelAsync(string username);
        Task<(bool Success, string Message, Models.Entities.Idea? CreatedIdea)> CreateIdeaAsync(CreateIdeaViewModel model, List<IFormFile>? files);
        Task<IQueryable<Models.Entities.Idea>> GetUserIdeasAsync(string username);
        Task<object?> GetEmployeeByBadgeNumberAsync(string badgeNumber);
        Task<IQueryable<Models.Entities.Idea>> GetAllIdeasQueryAsync(string username);
        Task<User?> GetUserByUsernameAsync(string username);
    }
}
