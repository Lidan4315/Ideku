using Ideku.Models.Entities;

namespace Ideku.Data.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByIdAsync(long id);
        Task<User?> GetUserByRoleAsync(string roleName);
        Task<User?> GetByEmployeeIdAsync(string employeeId);
        
        /// <summary>
        /// Get workstream leaders from specified divisions
        /// </summary>
        /// <param name="divisionIds">List of division IDs to search</param>
        /// <returns>List of active workstream leaders</returns>
        Task<List<User>> GetWorkstreamLeadersByDivisionsAsync(List<string> divisionIds);
    }
}
