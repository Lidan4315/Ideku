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

        // =================== USER MANAGEMENT OPERATIONS ===================

        /// <summary>
        /// Get all users with complete details (Employee, Role, Division, Department)
        /// Used for User Management list display
        /// </summary>
        Task<IEnumerable<User>> GetAllUsersWithDetailsAsync();

        /// <summary>
        /// Create new user account
        /// </summary>
        Task<User> CreateUserAsync(User user);

        /// <summary>
        /// Update existing user account
        /// </summary>
        Task<User> UpdateUserAsync(User user);

        /// <summary>
        /// Delete user account with dependency checking
        /// Returns false if user has dependencies (Ideas, WorkflowHistory, etc.)
        /// </summary>
        Task<bool> DeleteUserAsync(long id);

        /// <summary>
        /// Check if username already exists (for validation)
        /// excludeUserId parameter used during updates to exclude current user
        /// </summary>
        Task<bool> UsernameExistsAsync(string username, long? excludeUserId = null);


        /// <summary>
        /// Check if user can be safely deleted (no dependencies)
        /// Returns count of dependencies (Ideas, WorkflowHistory, Milestones)
        /// </summary>
        Task<int> GetUserDependenciesCountAsync(long userId);
    }

}
