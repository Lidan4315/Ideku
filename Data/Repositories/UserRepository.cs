using Microsoft.EntityFrameworkCore;
using Ideku.Data.Context;
using Ideku.Models.Entities;
using Ideku.Models.Statistics;

namespace Ideku.Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _context.Users
                .Include(u => u.Employee)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User?> GetByIdAsync(long id)
        {
            return await _context.Users
                .Include(u => u.Employee)
                    .ThenInclude(e => e.DivisionNavigation)
                .Include(u => u.Employee)
                    .ThenInclude(e => e.DepartmentNavigation)
                .Include(u => u.Role)
                .Include(u => u.CurrentRole)
                .Include(u => u.ActingDivision)
                .Include(u => u.ActingDepartment)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetByEmployeeIdAsync(string employeeId)
        {
            return await _context.Users
                .Include(u => u.Employee)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.EmployeeId == employeeId);
        }

        public async Task<List<User>> GetWorkstreamLeadersByDivisionsAsync(List<string> divisionIds)
        {
            if (!divisionIds?.Any() == true)
                return new List<User>();

            return await _context.Users
                .Include(u => u.Employee)
                    .ThenInclude(e => e.DivisionNavigation)
                .Include(u => u.Role)
                .Where(u => u.Role.RoleName == "Workstream Leader" &&
                           divisionIds.Contains(u.Employee.DIVISION) &&
                           u.Employee.EMP_STATUS == "Active")
                .OrderBy(u => u.Employee.DivisionNavigation.NameDivision)
                .ThenBy(u => u.Name)
                .ToListAsync();
        }

        public async Task<List<User>> GetWorkstreamLeadersByDepartmentAsync(string departmentId)
        {
            if (string.IsNullOrWhiteSpace(departmentId))
                return new List<User>();

            return await _context.Users
                .Include(u => u.Employee)
                    .ThenInclude(e => e.DepartmentNavigation)
                .Include(u => u.Employee)
                    .ThenInclude(e => e.DivisionNavigation)
                .Include(u => u.Role)
                .Where(u => u.Role.RoleName == "Workstream Leader" &&
                           u.Employee.DEPARTEMENT == departmentId &&
                           u.Employee.EMP_STATUS == "Active")
                .OrderBy(u => u.Name)
                .ToListAsync();
        }

        // =================== USER MANAGEMENT IMPLEMENTATIONS ===================

        /// <summary>
        /// Get all users with complete details for User Management display
        /// Includes Employee, Role, Division, and Department information
        /// </summary>
        public async Task<IEnumerable<User>> GetAllUsersWithDetailsAsync()
        {
            return await _context.Users
                .Include(u => u.Employee)
                    .ThenInclude(e => e.DivisionNavigation)
                .Include(u => u.Employee)
                    .ThenInclude(e => e.DepartmentNavigation)
                .Include(u => u.Role)
                .Include(u => u.CurrentRole)
                .Include(u => u.ActingDivision)
                .Include(u => u.ActingDepartment)
                .OrderByDescending(u => u.Id)
                .ToListAsync();
        }

        /// <summary>
        /// Get users as IQueryable for pagination - same pattern as IdeaRepository
        /// </summary>
        public async Task<IQueryable<User>> GetAllUsersQueryAsync()
        {
            // Return the query without executing it - this allows pagination to work at DB level
            return _context.Users
                .Include(u => u.Employee)
                    .ThenInclude(e => e.DivisionNavigation)
                .Include(u => u.Employee)
                    .ThenInclude(e => e.DepartmentNavigation)
                .Include(u => u.Role)
                .Include(u => u.CurrentRole)
                .Include(u => u.ActingDivision)
                .Include(u => u.ActingDepartment)
                .OrderByDescending(u => u.Id);
        }

        /// <summary>
        /// Create new user with automatic timestamp
        /// </summary>
        public async Task<User> CreateUserAsync(User user)
        {
            user.CreatedAt = DateTime.Now;
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            
            // Return user with all details loaded
            return await GetByIdAsync(user.Id) ?? user;
        }

        /// <summary>
        /// Update existing user with automatic timestamp
        /// </summary>
        public async Task<User> UpdateUserAsync(User user)
        {
            user.UpdatedAt = DateTime.Now;
            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            
            // Return updated user with all details loaded
            return await GetByIdAsync(user.Id) ?? user;
        }

        /// <summary>
        /// Delete user with dependency checking
        /// Returns false if user has dependencies or doesn't exist
        /// </summary>
        public async Task<bool> DeleteUserAsync(long id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            // Check dependencies before deletion
            var dependenciesCount = await GetUserDependenciesCountAsync(id);
            if (dependenciesCount > 0) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Check if username exists (excluding specific user ID for updates)
        /// Case-insensitive comparison for better user experience
        /// </summary>
        public async Task<bool> UsernameExistsAsync(string username, long? excludeUserId = null)
        {
            var query = _context.Users
                .Where(u => u.Username.ToLower() == username.ToLower());

            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.Id != excludeUserId.Value);
            }

            return await query.AnyAsync();
        }


        /// <summary>
        /// Count user dependencies to determine if user can be safely deleted
        /// Checks Ideas, WorkflowHistory, and Milestones
        /// </summary>
        public async Task<int> GetUserDependenciesCountAsync(long userId)
        {
            var ideasCount = await _context.Ideas
                .CountAsync(i => i.InitiatorUserId == userId);

            var workflowHistoryCount = await _context.WorkflowHistories
                .CountAsync(wh => wh.ActorUserId == userId);

            var milestonesCount = await _context.Milestones
                .Include(m => m.Idea)
                .ThenInclude(i => i.InitiatorUser)
                .CountAsync(m => m.Idea.InitiatorUserId == userId);

            return ideasCount + workflowHistoryCount + milestonesCount;
        }

        // =================== ACTING DURATION IMPLEMENTATIONS ===================

        /// <summary>
        /// Get all users who are currently acting
        /// Includes complete navigation properties for display
        /// </summary>
        public async Task<IEnumerable<User>> GetCurrentlyActingUsersAsync()
        {
            return await _context.Users
                .Include(u => u.Employee)
                    .ThenInclude(e => e.DivisionNavigation)
                .Include(u => u.Employee)
                    .ThenInclude(e => e.DepartmentNavigation)
                .Include(u => u.Role)
                .Include(u => u.CurrentRole)
                .Where(u => u.IsActing &&
                           u.ActingStartDate.HasValue &&
                           u.ActingEndDate.HasValue &&
                           DateTime.Now >= u.ActingStartDate.Value &&
                           DateTime.Now < u.ActingEndDate.Value)
                .OrderBy(u => u.ActingEndDate)
                .ToListAsync();
        }

        /// <summary>
        /// Get users whose acting period is about to expire within specified days
        /// Optimized query for notification purposes
        /// </summary>
        public async Task<IEnumerable<User>> GetActingUsersExpiringInDaysAsync(int withinDays)
        {
            var thresholdDate = DateTime.Now.AddDays(withinDays);

            return await _context.Users
                .Include(u => u.Employee)
                .Include(u => u.Role)
                .Include(u => u.CurrentRole)
                .Where(u => u.IsActing &&
                           u.ActingEndDate.HasValue &&
                           u.ActingEndDate.Value <= thresholdDate &&
                           u.ActingEndDate.Value > DateTime.Now)
                .OrderBy(u => u.ActingEndDate)
                .ToListAsync();
        }

        /// <summary>
        /// Get users whose acting period has expired and needs auto-revert
        /// Used by background service for efficient processing
        /// </summary>
        public async Task<IEnumerable<User>> GetExpiredActingUsersAsync()
        {
            return await _context.Users
                .Include(u => u.CurrentRole)
                .Where(u => u.IsActing &&
                           u.ActingEndDate.HasValue &&
                           u.ActingEndDate.Value <= DateTime.Now)
                .ToListAsync();
        }

        /// <summary>
        /// Get acting statistics for dashboard/reporting
        /// Returns counts for various acting states
        /// </summary>
        public async Task<ActingStatistics> GetActingStatisticsAsync()
        {
            var now = DateTime.Now;
            var totalUsers = await _context.Users.CountAsync();

            var currentlyActingUsers = await _context.Users
                .Where(u => u.IsActing &&
                           u.ActingStartDate.HasValue &&
                           u.ActingEndDate.HasValue &&
                           now >= u.ActingStartDate.Value &&
                           now < u.ActingEndDate.Value)
                .ToListAsync();

            var expiringIn7Days = currentlyActingUsers
                .Count(u => u.ActingEndDate!.Value <= now.AddDays(7));

            var expiringIn30Days = currentlyActingUsers
                .Count(u => u.ActingEndDate!.Value <= now.AddDays(30));

            var expiredActingUsers = await _context.Users
                .CountAsync(u => u.IsActing &&
                            u.ActingEndDate.HasValue &&
                            u.ActingEndDate.Value <= now);

            var urgentExpirations = currentlyActingUsers
                .Count(u => u.ActingEndDate!.Value <= now.AddDays(3));

            // Calculate average acting duration
            var averageDuration = currentlyActingUsers.Any()
                ? currentlyActingUsers.Average(u => (u.ActingEndDate!.Value - u.ActingStartDate!.Value).TotalDays)
                : 0;

            // Find most common acting role
            var roleGroups = await _context.Users
                .Where(u => u.IsActing && u.ActingStartDate.HasValue && u.ActingEndDate.HasValue &&
                           now >= u.ActingStartDate.Value && now < u.ActingEndDate.Value)
                .Include(u => u.Role)
                .GroupBy(u => u.Role.RoleName)
                .Select(g => new { RoleName = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .FirstOrDefaultAsync();

            return new ActingStatistics
            {
                TotalActingUsers = currentlyActingUsers.Count,
                TotalRegularUsers = totalUsers - currentlyActingUsers.Count,
                ExpiringIn7Days = expiringIn7Days,
                ExpiringIn30Days = expiringIn30Days,
                ExpiredActingUsers = expiredActingUsers,
                UrgentExpirations = urgentExpirations,
                HasUrgentExpirations = urgentExpirations > 0,
                AverageActingDurationDays = Math.Round(averageDuration, 1),
                MostCommonActingRole = roleGroups?.RoleName ?? "None",
                MostCommonActingRoleCount = roleGroups?.Count ?? 0
            };
        }

        /// <summary>
        /// Get users with specific acting role
        /// Useful for role-based queries and analysis
        /// </summary>
        public async Task<IEnumerable<User>> GetUsersByActingRoleAsync(int roleId)
        {
            var now = DateTime.Now;

            return await _context.Users
                .Include(u => u.Employee)
                    .ThenInclude(e => e.DivisionNavigation)
                .Include(u => u.Employee)
                    .ThenInclude(e => e.DepartmentNavigation)
                .Include(u => u.Role)
                .Include(u => u.CurrentRole)
                .Where(u => u.IsActing &&
                           u.RoleId == roleId &&
                           u.ActingStartDate.HasValue &&
                           u.ActingEndDate.HasValue &&
                           now >= u.ActingStartDate.Value &&
                           now < u.ActingEndDate.Value)
                .OrderBy(u => u.Employee.NAME)
                .ToListAsync();
        }
    }
}
