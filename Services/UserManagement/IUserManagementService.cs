using Ideku.Models.Entities;
using Ideku.Data.Repositories;

namespace Ideku.Services.UserManagement
{
    /// <summary>
    /// Service interface for User Management business operations
    /// Contains business logic, validation rules, and data coordination for User management
    /// </summary>
    public interface IUserManagementService
    {
        /// <summary>
        /// Get all users with complete details for display in management interface
        /// Business rule: Only return users for active employees
        /// </summary>
        Task<IEnumerable<User>> GetAllUsersAsync();

        /// <summary>
        /// Get user by ID with business logic validation
        /// </summary>
        Task<User?> GetUserByIdAsync(long id);


        /// <summary>
        /// Get all roles available for user assignment
        /// Business rule: Only return active/available roles
        /// </summary>
        Task<IEnumerable<Role>> GetAvailableRolesAsync();


        /// <summary>
        /// Create new user with comprehensive validation and business rules
        /// Returns tuple with success status, message, and created user
        /// </summary>
        Task<(bool Success, string Message, User? User)> CreateUserAsync(string employeeId, string username, int roleId, bool isActing = false);

        /// <summary>
        /// Update existing user with validation and business rules
        /// </summary>
        Task<(bool Success, string Message, User? User)> UpdateUserAsync(long userId, string username, int roleId, bool isActing);

        /// <summary>
        /// Delete user with comprehensive dependency checking and business rules
        /// </summary>
        Task<(bool Success, string Message)> DeleteUserAsync(long userId);

        /// <summary>
        /// Validate user data according to business rules
        /// Used for both create and update operations
        /// </summary>
        Task<(bool IsValid, string Message)> ValidateUserDataAsync(string username, string employeeId, long? excludeUserId = null);

        /// <summary>
        /// Get detailed information about user dependencies
        /// Used to provide detailed feedback when deletion is not possible
        /// </summary>
        Task<UserDependencyInfo> GetUserDependencyInfoAsync(long userId);

        /// <summary>
        /// Validate employee for user creation by Employee ID
        /// Business rule: Employee must be active and not have existing user account
        /// </summary>
        Task<(bool IsValid, string Message, Employee? Employee)> ValidateEmployeeForUserCreationAsync(string employeeId);
    }

    /// <summary>
    /// DTO containing detailed information about user dependencies
    /// Used for user-friendly deletion feedback
    /// </summary>
    public class UserDependencyInfo
    {
        public int IdeasCount { get; set; }
        public int WorkflowActionsCount { get; set; }
        public int MilestonesCount { get; set; }
        public int TotalDependencies => IdeasCount + WorkflowActionsCount + MilestonesCount;
        public bool CanDelete => TotalDependencies == 0;
        
        public string GetDependencyMessage()
        {
            if (CanDelete) return "User can be deleted safely.";
            
            var messages = new List<string>();
            if (IdeasCount > 0) messages.Add($"{IdeasCount} idea(s)");
            if (WorkflowActionsCount > 0) messages.Add($"{WorkflowActionsCount} workflow action(s)");
            if (MilestonesCount > 0) messages.Add($"{MilestonesCount} milestone(s)");
            
            return $"Cannot delete user. This user has created {string.Join(", ", messages)}. Please reassign or remove these items first.";
        }
    }
}