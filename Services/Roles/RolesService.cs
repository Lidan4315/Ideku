using Ideku.Data.Repositories;
using Ideku.Models.Entities;

namespace Ideku.Services.Roles
{
    /// <summary>
    /// Service implementation for Role business operations
    /// Contains validation, error handling, and business logic
    /// </summary>
    public class RolesService : IRolesService
    {
        private readonly IRolesRepository _rolesRepository;

        public RolesService(IRolesRepository rolesRepository)
        {
            _rolesRepository = rolesRepository;
        }

        /// <summary>
        /// Get all roles with business logic (e.g., ordering, filtering if needed)
        /// </summary>
        public async Task<IEnumerable<Role>> GetAllRolesAsync()
        {
            return await _rolesRepository.GetAllAsync();
        }

        /// <summary>
        /// Get role by ID with existence validation
        /// </summary>
        public async Task<Role?> GetRoleByIdAsync(int id)
        {
            if (id <= 0) return null;
            return await _rolesRepository.GetByIdAsync(id);
        }

        /// <summary>
        /// Create new role with comprehensive validation
        /// Returns tuple with success status, message, and created role
        /// </summary>
        public async Task<(bool Success, string Message, Role? Role)> CreateRoleAsync(string roleName, string? description = null)
        {
            // Validate input
            var validation = await ValidateRoleAsync(roleName);
            if (!validation.IsValid)
            {
                return (false, validation.Message, null);
            }

            try
            {
                // Create role entity
                var role = new Role
                {
                    RoleName = roleName.Trim(),
                    Desc = string.IsNullOrWhiteSpace(description) ? null : description.Trim()
                };

                // Save to database
                var createdRole = await _rolesRepository.AddAsync(role);
                return (true, "Role created successfully.", createdRole);
            }
            catch (Exception ex)
            {
                return (false, $"Error creating role: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Update existing role with validation
        /// </summary>
        public async Task<(bool Success, string Message, Role? Role)> UpdateRoleAsync(int id, string roleName, string? description = null)
        {
            // Check if role exists
            var existingRole = await _rolesRepository.GetByIdAsync(id);
            if (existingRole == null)
            {
                return (false, "Role not found.", null);
            }

            // Validate input (exclude current role from duplicate check)
            var validation = await ValidateRoleAsync(roleName, id);
            if (!validation.IsValid)
            {
                return (false, validation.Message, null);
            }

            try
            {
                // Update role properties
                existingRole.RoleName = roleName.Trim();
                existingRole.Desc = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

                // Save changes
                var updatedRole = await _rolesRepository.UpdateAsync(existingRole);
                return (true, "Role updated successfully.", updatedRole);
            }
            catch (Exception ex)
            {
                return (false, $"Error updating role: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Delete role with dependency checking and user-friendly messages
        /// </summary>
        public async Task<(bool Success, string Message)> DeleteRoleAsync(int id)
        {
            // Check if role exists
            var role = await _rolesRepository.GetByIdAsync(id);
            if (role == null)
            {
                return (false, "Role not found.");
            }

            // Check if role is in use
            var usageCount = await _rolesRepository.GetUsageCountAsync(id);
            if (usageCount > 0)
            {
                var userCount = await _rolesRepository.GetUserCountAsync(id);
                var approverRoleCount = await _rolesRepository.GetApproverRoleCountAsync(id);
                
                List<string> usageDetails = new List<string>();
                if (userCount > 0) usageDetails.Add($"{userCount} {(userCount == 1 ? "user" : "users")}");
                if (approverRoleCount > 0) usageDetails.Add($"{approverRoleCount} workflow {(approverRoleCount == 1 ? "approver" : "approvers")}");
                
                string usageText = string.Join(" and ", usageDetails);
                return (false, $"Cannot delete role. This role is used by {usageText}. Please remove all assignments first.");
            }

            try
            {
                var success = await _rolesRepository.DeleteAsync(id);
                if (success)
                {
                    return (true, "Role deleted successfully.");
                }
                else
                {
                    return (false, "Failed to delete role. Please try again.");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error deleting role: {ex.Message}");
            }
        }

        /// <summary>
        /// Comprehensive validation for role data
        /// </summary>
        public async Task<(bool IsValid, string Message)> ValidateRoleAsync(string roleName, int? excludeId = null)
        {
            // Check required fields
            if (string.IsNullOrWhiteSpace(roleName))
            {
                return (false, "Role name is required.");
            }

            // Check length constraints
            if (roleName.Trim().Length > 100)
            {
                return (false, "Role name cannot exceed 100 characters.");
            }

            // Check for invalid characters (optional business rule)
            if (roleName.Contains("<") || roleName.Contains(">"))
            {
                return (false, "Role name contains invalid characters.");
            }

            // Check for duplicate names
            var isDuplicate = await _rolesRepository.RoleNameExistsAsync(roleName.Trim(), excludeId);
            if (isDuplicate)
            {
                return (false, "A role with this name already exists.");
            }

            return (true, "Valid");
        }

        /// <summary>
        /// Get comprehensive statistics for role management dashboard
        /// </summary>
        public async Task<RoleStatistics> GetRoleStatisticsAsync()
        {
            var allRoles = await _rolesRepository.GetAllAsync();
            var stats = new RoleStatistics
            {
                TotalRoles = allRoles.Count()
            };

            // Calculate usage statistics
            var roleUsageCounts = new List<(string RoleName, int UserCount)>();
            int totalUsersAssigned = 0;
            int rolesWithNoUsers = 0;

            foreach (var role in allRoles)
            {
                var userCount = await _rolesRepository.GetUsageCountAsync(role.Id);
                roleUsageCounts.Add((role.RoleName, userCount));
                totalUsersAssigned += userCount;

                if (userCount == 0)
                {
                    rolesWithNoUsers++;
                }
            }

            stats.TotalUsersAssigned = totalUsersAssigned;
            stats.RolesWithNoUsers = rolesWithNoUsers;

            // Find most used role
            var mostUsed = roleUsageCounts.OrderByDescending(x => x.UserCount).FirstOrDefault();
            if (mostUsed != default)
            {
                stats.MostUsedRoleName = mostUsed.RoleName;
                stats.MostUsedRoleUserCount = mostUsed.UserCount;
            }

            return stats;
        }

        /// <summary>
        /// Get usage count for specific role
        /// </summary>
        public async Task<int> GetRoleUsageCountAsync(int roleId)
        {
            return await _rolesRepository.GetUsageCountAsync(roleId);
        }
    }
}