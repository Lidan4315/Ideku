using Ideku.ViewModels;
using Ideku.Models.Entities;

namespace Ideku.Services.Idea
{
    public interface IIdeaService
    {
        Task<CreateIdeaViewModel> PrepareCreateViewModelAsync(string username);
        Task<(bool Success, string Message, Models.Entities.Idea? CreatedIdea)> CreateIdeaAsync(CreateIdeaViewModel model, List<IFormFile>? files);
        /// <summary>
        /// Gets IQueryable for user's own ideas (for pagination support)
        /// </summary>
        /// <param name="username">Username of the user</param>
        /// <returns>IQueryable of Ideas by the user</returns>
        Task<IQueryable<Models.Entities.Idea>> GetUserIdeasAsync(string username);
        Task<object?> GetEmployeeByBadgeNumberAsync(string badgeNumber);
        
        /// <summary>
        /// Gets IQueryable for all ideas based on user role filtering
        /// - Superuser: sees all ideas
        /// - Workstream Leader: sees ideas for their division/department + RelatedDivisions
        /// - Others: implementation pending
        /// </summary>
        /// <param name="username">Username of the user</param>
        /// <returns>IQueryable of Ideas filtered by user role</returns>
        Task<IQueryable<Models.Entities.Idea>> GetAllIdeasQueryAsync(string username);
        
        /// <summary>
        /// Gets user by username
        /// </summary>
        /// <param name="username">Username</param>
        /// <returns>User entity</returns>
        Task<User?> GetUserByUsernameAsync(string username);
    }
}
