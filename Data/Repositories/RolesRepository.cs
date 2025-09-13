using Microsoft.EntityFrameworkCore;
using Ideku.Data.Context;
using Ideku.Models.Entities;

namespace Ideku.Data.Repositories
{
    /// <summary>
    /// Repository implementation for Roles entity
    /// Handles all database operations with proper error handling
    /// </summary>
    public class RolesRepository : IRolesRepository
    {
        private readonly AppDbContext _context;

        public RolesRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all roles ordered by ID for consistent UI display
        /// </summary>
        public async Task<IEnumerable<Role>> GetAllAsync()
        {
            return await _context.Roles
                .OrderBy(r => r.Id)
                .ToListAsync();
        }

        /// <summary>
        /// Get role by ID for edit/detail operations
        /// Returns null if not found (handled gracefully)
        /// </summary>
        public async Task<Role?> GetByIdAsync(int id)
        {
            return await _context.Roles
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        /// <summary>
        /// Add new role to database
        /// Sets CreatedAt timestamp automatically
        /// </summary>
        public async Task<Role> AddAsync(Role role)
        {
            role.CreatedAt = DateTime.Now;
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();
            return role;
        }

        /// <summary>
        /// Update existing role
        /// Sets UpdatedAt timestamp automatically
        /// </summary>
        public async Task<Role> UpdateAsync(Role role)
        {
            role.UpdatedAt = DateTime.Now;
            _context.Entry(role).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return role;
        }

        /// <summary>
        /// Delete role with safety checks
        /// Returns false if role has dependencies or doesn't exist
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null) return false;

            // Check if role is being used by users
            var usageCount = await GetUsageCountAsync(id);
            if (usageCount > 0) return false;

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Check if role exists for validation purposes
        /// </summary>
        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Roles
                .AnyAsync(r => r.Id == id);
        }

        /// <summary>
        /// Check for duplicate role names
        /// excludeId is used during updates to exclude current role
        /// </summary>
        public async Task<bool> RoleNameExistsAsync(string roleName, int? excludeId = null)
        {
            var query = _context.Roles
                .Where(r => r.RoleName.ToLower() == roleName.ToLower());

            if (excludeId.HasValue)
            {
                query = query.Where(r => r.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        /// <summary>
        /// Count all usage of this role (Users + ApproverRoles)
        /// Used to prevent deletion of roles still in use
        /// </summary>
        public async Task<int> GetUsageCountAsync(int roleId)
        {
            var userCount = await GetUserCountAsync(roleId);
            var approverRoleCount = await GetApproverRoleCountAsync(roleId);
            
            return userCount + approverRoleCount;
        }

        /// <summary>
        /// Count users assigned to this role specifically
        /// </summary>
        public async Task<int> GetUserCountAsync(int roleId)
        {
            return await _context.Users
                .CountAsync(u => u.RoleId == roleId);
        }

        /// <summary>
        /// Count approver roles using this role
        /// </summary>
        public async Task<int> GetApproverRoleCountAsync(int roleId)
        {
            return await _context.ApproverRoles
                .CountAsync(ar => ar.RoleId == roleId);
        }
    }
}