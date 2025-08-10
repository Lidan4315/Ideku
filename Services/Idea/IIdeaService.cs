using Ideku.ViewModels;
using Ideku.Models.Entities;

namespace Ideku.Services.Idea
{
    public interface IIdeaService
    {
        Task<CreateIdeaViewModel> PrepareCreateViewModelAsync(string username);
        Task<(bool Success, string Message, Models.Entities.Idea? CreatedIdea)> CreateIdeaAsync(CreateIdeaViewModel model, List<IFormFile>? files);
        Task<IEnumerable<Models.Entities.Idea>> GetUserIdeasAsync(string username);
        Task<List<object>> GetDepartmentsByDivisionAsync(string divisionId);
        Task<object?> GetEmployeeByBadgeNumberAsync(string badgeNumber);
    }
}
