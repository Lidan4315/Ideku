using Ideku.Models.Entities;
using Ideku.Models.Statistics;

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
        /// Get all users as IQueryable for pagination and filtering
        /// Includes same navigation properties as GetAllUsersWithDetailsAsync
        /// Same pattern as IdeaRepository for consistency
        /// </summary>
        Task<IQueryable<User>> GetAllUsersQueryAsync();

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

        // =================== ACTING DURATION QUERIES ===================

        /// <summary>
        /// Get all users who are currently acting
        /// Includes complete navigation properties for display
        /// </summary>
        Task<IEnumerable<User>> GetCurrentlyActingUsersAsync();

        /// <summary>
        /// Get users whose acting period is about to expire within specified days
        /// Optimized query for notification purposes
        /// </summary>
        Task<IEnumerable<User>> GetActingUsersExpiringInDaysAsync(int withinDays);

        /// <summary>
        /// Get users whose acting period has expired and needs auto-revert
        /// Used by background service for efficient processing
        /// </summary>
        Task<IEnumerable<User>> GetExpiredActingUsersAsync();

        /// <summary>
        /// Get acting statistics for dashboard/reporting
        /// Returns counts for various acting states
        /// </summary>
        Task<ActingStatistics> GetActingStatisticsAsync();

        /// <summary>
        /// Get users with specific acting role
        /// Useful for role-based queries and analysis
        /// </summary>
        Task<IEnumerable<User>> GetUsersByActingRoleAsync(int roleId);
    }

}
