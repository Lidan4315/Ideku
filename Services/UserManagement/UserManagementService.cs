using Ideku.Data.Repositories;
using Ideku.Models.Entities;
using Ideku.Models.Statistics;

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

                // BUSINESS RULE: Cannot change role while user is acting
                if (existingUser.IsCurrentlyActing() && roleId != existingUser.CurrentRoleId)
                {
                    return (false, "Cannot change role while user is acting. Please stop acting first or wait until acting period expires.", null);
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

        // =================== ACTING DURATION MANAGEMENT ===================


        /// <summary>
        /// Set user to acting role with specific duration
        /// </summary>
        public async Task<(bool Success, string Message)> SetUserActingAsync(
            long userId,
            int actingRoleId,
            DateTime startDate,
            DateTime endDate)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return (false, "User not found.");
                }

                // Validate acting data
                var validation = await ValidateActingDataAsync(user.RoleId, actingRoleId, startDate, endDate);
                if (!validation.IsValid)
                {
                    return (false, validation.Message);
                }

                // Set acting
                await SetUserActingInternalAsync(user, actingRoleId, startDate, endDate);
                await _userRepository.UpdateUserAsync(user);

                var actingRole = await _rolesRepository.GetByIdAsync(actingRoleId);
                return (true, $"User set to acting {actingRole?.RoleName} until {endDate:MMM dd, yyyy}");
            }
            catch (Exception ex)
            {
                return (false, $"Error setting user acting: {ex.Message}");
            }
        }

        /// <summary>
        /// Stop user acting immediately and revert to original role
        /// </summary>
        public async Task<(bool Success, string Message)> StopUserActingAsync(long userId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return (false, "User not found.");
                }

                if (!user.IsActing || !user.CurrentRoleId.HasValue)
                {
                    return (false, "User is not currently acting.");
                }

                // Stop acting
                await StopUserActingInternalAsync(user);
                await _userRepository.UpdateUserAsync(user);

                return (true, "User acting stopped and reverted to original role.");
            }
            catch (Exception ex)
            {
                return (false, $"Error stopping user acting: {ex.Message}");
            }
        }

        /// <summary>
        /// Extend user acting period to new end date
        /// </summary>
        public async Task<(bool Success, string Message)> ExtendUserActingAsync(long userId, DateTime newEndDate)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return (false, "User not found.");
                }

                if (!user.IsActing || !user.ActingEndDate.HasValue)
                {
                    return (false, "User is not currently acting.");
                }

                if (newEndDate <= user.ActingEndDate.Value)
                {
                    return (false, "New end date must be after current end date.");
                }

                if (newEndDate <= DateTime.Now)
                {
                    return (false, "New end date must be in the future.");
                }

                // Extend acting period
                user.ActingEndDate = newEndDate;
                user.UpdatedAt = DateTime.Now;

                await _userRepository.UpdateUserAsync(user);
                return (true, $"Acting period extended until {newEndDate:MMM dd, yyyy}");
            }
            catch (Exception ex)
            {
                return (false, $"Error extending acting period: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all users whose acting period is about to expire
        /// </summary>
        public async Task<IEnumerable<User>> GetExpiringActingUsersAsync(int withinDays = 7)
        {
            return await _userRepository.GetActingUsersExpiringInDaysAsync(withinDays);
        }

        /// <summary>
        /// Get all users whose acting period has expired and needs auto-revert
        /// </summary>
        public async Task<IEnumerable<User>> GetExpiredActingUsersAsync()
        {
            return await _userRepository.GetExpiredActingUsersAsync();
        }

        /// <summary>
        /// Auto-revert expired acting users (used by background service)
        /// </summary>
        public async Task<(int ProcessedCount, List<string> Messages)> ProcessExpiredActingUsersAsync()
        {
            var messages = new List<string>();
            var processedCount = 0;

            try
            {
                var expiredUsers = await GetExpiredActingUsersAsync();

                foreach (var user in expiredUsers)
                {
                    try
                    {
                        if (user.CurrentRoleId.HasValue)
                        {
                            var originalRole = await _rolesRepository.GetByIdAsync(user.CurrentRoleId.Value);
                            var actingRole = await _rolesRepository.GetByIdAsync(user.RoleId);

                            // Stop acting
                            await StopUserActingInternalAsync(user);
                            await _userRepository.UpdateUserAsync(user);

                            messages.Add($"User {user.Name} reverted from {actingRole?.RoleName} to {originalRole?.RoleName}");
                            processedCount++;
                        }
                        else
                        {
                            messages.Add($"Warning: User {user.Name} acting but no original role found");
                        }
                    }
                    catch (Exception ex)
                    {
                        messages.Add($"Error reverting user {user.Name}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                messages.Add($"Error processing expired acting users: {ex.Message}");
            }

            return (processedCount, messages);
        }

        /// <summary>
        /// Get acting statistics for dashboard and reporting
        /// Returns comprehensive statistics about acting users
        /// </summary>
        public async Task<ActingStatistics> GetActingStatisticsAsync()
        {
            return await _userRepository.GetActingStatisticsAsync();
        }

        // =================== PRIVATE HELPER METHODS FOR ACTING ===================

        /// <summary>
        /// Internal method to set user acting (used by multiple public methods)
        /// </summary>
        private async Task SetUserActingInternalAsync(User user, int actingRoleId, DateTime startDate, DateTime endDate)
        {
            // Backup original role if not already acting
            if (!user.IsActing || !user.CurrentRoleId.HasValue)
            {
                user.CurrentRoleId = user.RoleId;
            }

            // Set acting
            user.RoleId = actingRoleId;
            user.ActingStartDate = startDate;
            user.ActingEndDate = endDate;
            user.IsActing = true;
            user.UpdatedAt = DateTime.Now;
        }

        /// <summary>
        /// Internal method to stop user acting (used by multiple public methods)
        /// </summary>
        private async Task StopUserActingInternalAsync(User user)
        {
            if (user.CurrentRoleId.HasValue)
            {
                user.RoleId = user.CurrentRoleId.Value;
            }

            user.IsActing = false;
            user.CurrentRoleId = null;
            user.ActingStartDate = null;
            user.ActingEndDate = null;
            user.UpdatedAt = DateTime.Now;
        }

        /// <summary>
        /// Validate acting duration data
        /// </summary>
        private async Task<(bool IsValid, string Message)> ValidateActingDataAsync(
            int currentRoleId,
            int? actingRoleId,
            DateTime? startDate,
            DateTime? endDate)
        {
            if (!actingRoleId.HasValue)
            {
                return (false, "Acting role is required when setting acting position.");
            }

            if (!startDate.HasValue || !endDate.HasValue)
            {
                return (false, "Acting start date and end date are required.");
            }

            if (startDate.Value >= endDate.Value)
            {
                return (false, "Acting end date must be after start date.");
            }

            if (endDate.Value <= DateTime.Now.AddHours(1))
            {
                return (false, "Acting end date must be at least 1 hour in the future.");
            }

            if (actingRoleId.Value == currentRoleId)
            {
                return (false, "Acting role must be different from current role.");
            }

            // Check if acting role exists
            var actingRole = await _rolesRepository.GetByIdAsync(actingRoleId.Value);
            if (actingRole == null)
            {
                return (false, "Acting role not found.");
            }

            // Max acting duration validation (1 year)
            var duration = endDate.Value - startDate.Value;
            if (duration.TotalDays > 365)
            {
                return (false, "Acting duration cannot exceed 1 year.");
            }

            return (true, "Acting data is valid.");
        }
    }
}