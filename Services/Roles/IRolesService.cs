using Ideku.Models.Entities;

namespace Ideku.Services.Roles
{
    /// <summary>
    /// Service interface for Role business operations
    /// Contains business logic and validation rules for Role management
    /// </summary>
    public interface IRolesService
    {
        /// <summary>
        /// Get all roles for display in management interface
        /// </summary>
        Task<IEnumerable<Role>> GetAllRolesAsync();

        /// <summary>
        /// Get role by ID with validation
        /// </summary>
        Task<Role?> GetRoleByIdAsync(int id);

        /// <summary>
        /// Create new role with business validation
        /// </summary>
        Task<(bool Success, string Message, Role? Role)> CreateRoleAsync(string roleName, string? description = null);

        /// <summary>
        /// Update existing role with validation
        /// </summary>
        Task<(bool Success, string Message, Role? Role)> UpdateRoleAsync(int id, string roleName, string? description = null);

        /// <summary>
        /// Delete role with dependency checking
        /// </summary>
        Task<(bool Success, string Message)> DeleteRoleAsync(int id);

        /// <summary>
        /// Validate role data for business rules
        /// </summary>
        Task<(bool IsValid, string Message)> ValidateRoleAsync(string roleName, int? excludeId = null);

        /// <summary>
        /// Get role statistics for dashboard
        /// </summary>
        Task<RoleStatistics> GetRoleStatisticsAsync();

        /// <summary>
        /// Get usage count for specific role
        /// </summary>
        Task<int> GetRoleUsageCountAsync(int roleId);
    }

    /// <summary>
    /// DTO for role statistics display
    /// </summary>
    public class RoleStatistics
    {
        public int TotalRoles { get; set; }
        public int TotalUsersAssigned { get; set; }
        public int RolesWithNoUsers { get; set; }
        public string MostUsedRoleName { get; set; } = string.Empty;
        public int MostUsedRoleUserCount { get; set; }
    }
}