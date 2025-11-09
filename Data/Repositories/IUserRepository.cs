using Ideku.Models.Entities;
using Ideku.Models.Statistics;

namespace Ideku.Data.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByIdAsync(long id);
        Task<User?> GetByEmployeeIdAsync(string employeeId);
        Task<List<User>> GetWorkstreamLeadersByDivisionsAsync(List<string> divisionIds);
        Task<List<User>> GetWorkstreamLeadersByDepartmentAsync(string departmentId);

        // USER MANAGEMENT OPERATIONS
        Task<IEnumerable<User>> GetAllUsersWithDetailsAsync();
        Task<IQueryable<User>> GetAllUsersQueryAsync();
        Task<User> CreateUserAsync(User user);
        Task<User> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(long id);
        Task<bool> UsernameExistsAsync(string username, long? excludeUserId = null);
        Task<int> GetUserDependenciesCountAsync(long userId);

        // ACTING DURATION QUERIES
        Task<IEnumerable<User>> GetCurrentlyActingUsersAsync();

        /// Get users whose acting period is about to expire within specified days
        Task<IEnumerable<User>> GetActingUsersExpiringInDaysAsync(int withinDays);

        /// Get users whose acting period has expired and needs auto-revert
        Task<IEnumerable<User>> GetExpiredActingUsersAsync();

        /// Get acting statistics for dashboard/reporting
        Task<ActingStatistics> GetActingStatisticsAsync();

        /// Get users with specific acting role
        Task<IEnumerable<User>> GetUsersByActingRoleAsync(int roleId);

        /// Get all users with specific role (including acting users)
        Task<IEnumerable<User>> GetUsersByRoleAsync(int roleId);
    }

}
