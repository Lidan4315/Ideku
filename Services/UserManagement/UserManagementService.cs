using Ideku.Data.Repositories;
using Ideku.Models.Entities;

namespace Ideku.Services.UserManagement
{
    /// <summary>
    /// Service implementation for User Management business operations
    /// Contains validation, error handling, business logic, and data coordination
    /// </summary>
    public class UserManagementService : IUserManagementService
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IRolesRepository _rolesRepository;

        public UserManagementService(
            IUserRepository userRepository, 
            IEmployeeRepository employeeRepository,
            IRolesRepository rolesRepository)
        {
            _userRepository = userRepository;
            _employeeRepository = employeeRepository;
            _rolesRepository = rolesRepository;
        }

        /// <summary>
        /// Get all users with business filtering
        /// Business rule: Return all users ordered by name for consistent display
        /// </summary>
        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _userRepository.GetAllUsersWithDetailsAsync();
        }

        /// <summary>
        /// Get users as queryable for pagination - same pattern as IdeaService
        /// </summary>
        public async Task<IQueryable<User>> GetAllUsersQueryAsync()
        {
            return await _userRepository.GetAllUsersQueryAsync();
        }

        /// <summary>
        /// Get user by ID with validation
        /// </summary>
        public async Task<User?> GetUserByIdAsync(long id)
        {
            if (id <= 0) return null;
            return await _userRepository.GetByIdAsync(id);
        }


        /// <summary>
        /// Get available roles for user assignment
        /// Business rule: Return all active roles
        /// </summary>
        public async Task<IEnumerable<Role>> GetAvailableRolesAsync()
        {
            return await _rolesRepository.GetAllAsync();
        }


        /// <summary>
        /// Create new user with comprehensive validation and business rules
        /// </summary>
        public async Task<(bool Success, string Message, User? User)> CreateUserAsync(string employeeId, string username, int roleId, bool isActing = false)
        {
            try
            {
                // Validate input data
                var validation = await ValidateUserDataAsync(username, employeeId);
                if (!validation.IsValid)
                {
                    return (false, validation.Message, null);
                }

                // Verify employee exists and is active
                var employee = await _employeeRepository.GetByEmployeeIdAsync(employeeId);
                if (employee == null)
                {
                    return (false, "Selected employee not found or inactive.", null);
                }

                // Verify role exists
                var role = await _rolesRepository.GetByIdAsync(roleId);
                if (role == null)
                {
                    return (false, "Selected role not found.", null);
                }

                // Business rule: Employee can only have one user account
                var existingUser = await _userRepository.GetByEmployeeIdAsync(employeeId);
                if (existingUser != null)
                {
                    return (false, "This employee already has a user account.", null);
                }

                // Create user entity
                var user = new User
                {
                    EmployeeId = employeeId,
                    Username = username.Trim(),
                    Name = employee.NAME, // Auto-populate from employee data
                    RoleId = roleId,
                    IsActing = isActing
                };

                // Save to database
                var createdUser = await _userRepository.CreateUserAsync(user);
                return (true, "User created successfully.", createdUser);
            }
            catch (Exception ex)
            {
                return (false, $"Error creating user: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Update existing user with validation and business rules
        /// </summary>
        public async Task<(bool Success, string Message, User? User)> UpdateUserAsync(long userId, string username, int roleId, bool isActing)
        {
            try
            {
                // Check if user exists
                var existingUser = await _userRepository.GetByIdAsync(userId);
                if (existingUser == null)
                {
                    return (false, "User not found.", null);
                }

                // Validate input data (exclude current user from username check)
                var validation = await ValidateUserDataAsync(username, existingUser.EmployeeId, userId);
                if (!validation.IsValid)
                {
                    return (false, validation.Message, null);
                }

                // Verify role exists
                var role = await _rolesRepository.GetByIdAsync(roleId);
                if (role == null)
                {
                    return (false, "Selected role not found.", null);
                }

                // Update user properties
                existingUser.Username = username.Trim();
                existingUser.RoleId = roleId;
                existingUser.IsActing = isActing;
                // Note: EmployeeId and Name should not be changed - business rule

                // Save changes
                var updatedUser = await _userRepository.UpdateUserAsync(existingUser);
                return (true, "User updated successfully.", updatedUser);
            }
            catch (Exception ex)
            {
                return (false, $"Error updating user: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Delete user with comprehensive dependency checking
        /// </summary>
        public async Task<(bool Success, string Message)> DeleteUserAsync(long userId)
        {
            try
            {
                // Check if user exists
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return (false, "User not found.");
                }

                // Get detailed dependency information
                var dependencyInfo = await GetUserDependencyInfoAsync(userId);
                if (!dependencyInfo.CanDelete)
                {
                    return (false, dependencyInfo.GetDependencyMessage());
                }

                // Perform deletion
                var success = await _userRepository.DeleteUserAsync(userId);
                if (success)
                {
                    return (true, "User deleted successfully.");
                }
                else
                {
                    return (false, "Failed to delete user. Please try again.");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error deleting user: {ex.Message}");
            }
        }

        /// <summary>
        /// Comprehensive validation for user data according to business rules
        /// </summary>
        public async Task<(bool IsValid, string Message)> ValidateUserDataAsync(string username, string employeeId, long? excludeUserId = null)
        {
            // Required field validation
            if (string.IsNullOrWhiteSpace(username))
            {
                return (false, "Username is required.");
            }

            if (string.IsNullOrWhiteSpace(employeeId))
            {
                return (false, "Employee selection is required.");
            }

            // Username format validation
            username = username.Trim();
            if (username.Length < 3)
            {
                return (false, "Username must be at least 3 characters long.");
            }

            if (username.Length > 100)
            {
                return (false, "Username cannot exceed 100 characters.");
            }

            // Business rule: Username should not contain special characters that might cause issues
            if (username.Contains(" ") || username.Contains("<") || username.Contains(">"))
            {
                return (false, "Username cannot contain spaces or special characters like < >.");
            }

            // Check for duplicate username
            var usernameExists = await _userRepository.UsernameExistsAsync(username, excludeUserId);
            if (usernameExists)
            {
                return (false, "This username is already taken. Please choose a different one.");
            }

            return (true, "Valid");
        }

        /// <summary>
        /// Get detailed information about user dependencies
        /// Provides comprehensive information for deletion decisions
        /// </summary>
        public async Task<UserDependencyInfo> GetUserDependencyInfoAsync(long userId)
        {
            // This method would ideally call separate methods to count each type of dependency
            // For now, we'll use the combined count method and break it down
            var totalCount = await _userRepository.GetUserDependenciesCountAsync(userId);
            
            // In a real implementation, you might want to call separate repository methods
            // to get detailed counts for each dependency type for better user feedback
            
            // For this implementation, we'll create a simplified version
            // In production, you'd want to add specific count methods to the repository
            return new UserDependencyInfo
            {
                IdeasCount = totalCount > 0 ? totalCount : 0, // Simplified - in real app, get actual counts
                WorkflowActionsCount = 0,
                MilestonesCount = 0
            };
        }

        /// <summary>
        /// Validate employee for user creation by Employee ID
        /// Comprehensive validation including business rules
        /// </summary>
        public async Task<(bool IsValid, string Message, Employee? Employee)> ValidateEmployeeForUserCreationAsync(string employeeId)
        {
            try
            {
                // Input validation
                if (string.IsNullOrWhiteSpace(employeeId))
                {
                    return (false, "Employee ID is required.", null);
                }

                employeeId = employeeId.Trim().ToUpper();

                // Check if employee exists and is active
                var employee = await _employeeRepository.GetByEmployeeIdAsync(employeeId);
                if (employee == null)
                {
                    return (false, "Employee ID not found or employee is inactive.", null);
                }

                // Business rule: Employee can only have one user account
                var existingUser = await _userRepository.GetByEmployeeIdAsync(employeeId);
                if (existingUser != null)
                {
                    return (false, $"Employee {employee.NAME} already has a user account.", null);
                }

                // All validations passed
                return (true, "Employee is valid and available for user creation.", employee);
            }
            catch (Exception ex)
            {
                return (false, $"Error validating employee: {ex.Message}", null);
            }
        }
    }
}