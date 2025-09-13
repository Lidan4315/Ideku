using Microsoft.EntityFrameworkCore;
using Ideku.Data.Context;
using Ideku.Models.Entities;

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
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetUserByRoleAsync(string roleName)
        {
            return await _context.Users
                .Include(u => u.Employee)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Role.RoleName == roleName);
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
                .OrderByDescending(u => u.Id)
                .ToListAsync();
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
                .CountAsync(m => m.CreatorUserId == userId);

            return ideasCount + workflowHistoryCount + milestonesCount;
        }
    }
}
