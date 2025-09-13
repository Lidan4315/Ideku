using Ideku.Models.Entities;

namespace Ideku.Data.Repositories
{
    /// <summary>
    /// Repository interface for Roles entity operations
    /// Defines all database operations needed for Role management
    /// </summary>
    public interface IRolesRepository
    {
        /// <summary>
        /// Get all roles ordered by name
        /// </summary>
        Task<IEnumerable<Role>> GetAllAsync();

        /// <summary>
        /// Get role by ID with error handling
        /// </summary>
        Task<Role?> GetByIdAsync(int id);

        /// <summary>
        /// Add new role to database
        /// </summary>
        Task<Role> AddAsync(Role role);

        /// <summary>
        /// Update existing role
        /// </summary>
        Task<Role> UpdateAsync(Role role);

        /// <summary>
        /// Delete role by ID
        /// Returns false if role doesn't exist or has dependencies
        /// </summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Check if role exists by ID
        /// </summary>
        Task<bool> ExistsAsync(int id);

        /// <summary>
        /// Check if role name already exists (for duplicate validation)
        /// excludeId parameter is used for update operations
        /// </summary>
        Task<bool> RoleNameExistsAsync(string roleName, int? excludeId = null);

        /// <summary>
        /// Get count of users assigned to this role
        /// Used to prevent deletion of roles that are still in use
        /// </summary>
        Task<int> GetUsageCountAsync(int roleId);

        /// <summary>
        /// Get count of users assigned to this role specifically
        /// </summary>
        Task<int> GetUserCountAsync(int roleId);

        /// <summary>
        /// Get count of approver roles using this role
        /// </summary>
        Task<int> GetApproverRoleCountAsync(int roleId);
    }
}