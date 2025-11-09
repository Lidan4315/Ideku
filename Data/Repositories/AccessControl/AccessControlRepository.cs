using Microsoft.EntityFrameworkCore;
using Ideku.Data.Context;
using Ideku.Models.Entities;

namespace Ideku.Data.Repositories.AccessControl
{
    public class AccessControlRepository : IAccessControlRepository
    {
        private readonly AppDbContext _context;

        public AccessControlRepository(AppDbContext context)
        {
            _context = context;
        }

        // MODULE QUERIES

        public async Task<IEnumerable<Module>> GetAllModulesAsync()
        {
            return await _context.Modules
                .Where(m => m.IsActive)
                .OrderBy(m => m.SortOrder)
                .ToListAsync();
        }

        public async Task<Module?> GetModuleByIdAsync(int id)
        {
            return await _context.Modules
                .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);
        }

        public async Task<Module?> GetModuleByKeyAsync(string moduleKey)
        {
            return await _context.Modules
                .FirstOrDefaultAsync(m => m.ModuleKey == moduleKey && m.IsActive);
        }

        public async Task<Module?> GetModuleByControllerAsync(string controllerName)
        {
            return await _context.Modules
                .FirstOrDefaultAsync(m => m.ControllerName == controllerName && m.IsActive);
        }

        // PERMISSION QUERIES

        public async Task<bool> HasAccessToModuleAsync(int roleId, string moduleKey)
        {
            return await _context.RoleAccessModules
                .AnyAsync(ram =>
                    ram.RoleId == roleId &&
                    ram.Module.ModuleKey == moduleKey &&
                    ram.CanAccess &&
                    ram.Module.IsActive);
        }

        public async Task<bool> HasAccessToControllerAsync(int roleId, string controllerName)
        {
            return await _context.RoleAccessModules
                .AnyAsync(ram =>
                    ram.RoleId == roleId &&
                    ram.Module.ControllerName == controllerName &&
                    ram.CanAccess &&
                    ram.Module.IsActive);
        }

        public async Task<IEnumerable<Module>> GetAccessibleModulesAsync(int roleId)
        {
            return await _context.RoleAccessModules
                .Where(ram =>
                    ram.RoleId == roleId &&
                    ram.CanAccess &&
                    ram.Module.IsActive)
                .Include(ram => ram.Module)
                .Select(ram => ram.Module)
                .OrderBy(m => m.SortOrder)
                .ToListAsync();
        }

        public async Task<IEnumerable<string>> GetAccessibleModuleKeysAsync(int roleId)
        {
            return await _context.RoleAccessModules
                .Where(ram =>
                    ram.RoleId == roleId &&
                    ram.CanAccess &&
                    ram.Module.IsActive)
                .Select(ram => ram.Module.ModuleKey)
                .Distinct()
                .ToListAsync();
        }

        // ROLE ACCESS MODULE MANAGEMENT

        public async Task<IEnumerable<RoleAccessModule>> GetRoleAccessModulesAsync(int roleId)
        {
            return await _context.RoleAccessModules
                .Where(ram => ram.RoleId == roleId)
                .Include(ram => ram.Module)
                .Include(ram => ram.Role)
                .ToListAsync();
        }

        public async Task<RoleAccessModule?> GetRoleAccessModuleAsync(int roleId, int moduleId)
        {
            return await _context.RoleAccessModules
                .FirstOrDefaultAsync(ram =>
                    ram.RoleId == roleId &&
                    ram.ModuleId == moduleId);
        }

        public async Task<RoleAccessModule> GrantAccessAsync(int roleId, int moduleId, long? modifiedBy)
        {
            var existing = await GetRoleAccessModuleAsync(roleId, moduleId);

            if (existing != null)
            {
                // Update existing
                existing.CanAccess = true;
                existing.ModifiedBy = modifiedBy;
                existing.ModifiedAt = DateTime.Now;

                _context.RoleAccessModules.Update(existing);
            }
            else
            {
                // Create new
                existing = new RoleAccessModule
                {
                    RoleId = roleId,
                    ModuleId = moduleId,
                    CanAccess = true,
                    ModifiedBy = modifiedBy,
                    ModifiedAt = DateTime.Now
                };

                await _context.RoleAccessModules.AddAsync(existing);
            }

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task RevokeAccessAsync(int roleId, int moduleId, long? modifiedBy)
        {
            var existing = await GetRoleAccessModuleAsync(roleId, moduleId);

            if (existing != null)
            {
                existing.CanAccess = false;
                existing.ModifiedBy = modifiedBy;
                existing.ModifiedAt = DateTime.Now;

                _context.RoleAccessModules.Update(existing);
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateRoleAccessModulesAsync(int roleId, List<int> moduleIds, long? modifiedBy)
        {
            // Get all modules
            var allModules = await _context.Modules.Select(m => m.Id).ToListAsync();

            // Get existing access records
            var existingAccess = await _context.RoleAccessModules
                .Where(ram => ram.RoleId == roleId)
                .ToListAsync();

            // Determine which modules to grant and revoke
            var toGrant = moduleIds.Except(existingAccess.Where(e => e.CanAccess).Select(e => e.ModuleId)).ToList();
            var toRevoke = existingAccess.Where(e => e.CanAccess && !moduleIds.Contains(e.ModuleId)).Select(e => e.ModuleId).ToList();

            // Grant new access
            foreach (var moduleId in toGrant)
            {
                var existing = existingAccess.FirstOrDefault(e => e.ModuleId == moduleId);

                if (existing != null)
                {
                    existing.CanAccess = true;
                    existing.ModifiedBy = modifiedBy;
                    existing.ModifiedAt = DateTime.Now;
                    _context.RoleAccessModules.Update(existing);
                }
                else
                {
                    await _context.RoleAccessModules.AddAsync(new RoleAccessModule
                    {
                        RoleId = roleId,
                        ModuleId = moduleId,
                        CanAccess = true,
                        ModifiedBy = modifiedBy,
                        ModifiedAt = DateTime.Now
                    });
                }
            }

            // Revoke access
            foreach (var moduleId in toRevoke)
            {
                var existing = existingAccess.FirstOrDefault(e => e.ModuleId == moduleId);

                if (existing != null)
                {
                    existing.CanAccess = false;
                    existing.ModifiedBy = modifiedBy;
                    existing.ModifiedAt = DateTime.Now;
                    _context.RoleAccessModules.Update(existing);
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<Dictionary<int, Dictionary<int, bool>>> GetPermissionMatrixAsync()
        {
            var matrix = new Dictionary<int, Dictionary<int, bool>>();

            var allRoles = await _context.Roles.ToListAsync();
            var allModules = await _context.Modules.Where(m => m.IsActive).ToListAsync();
            var allAccess = await _context.RoleAccessModules.ToListAsync();

            foreach (var role in allRoles)
            {
                matrix[role.Id] = new Dictionary<int, bool>();

                foreach (var module in allModules)
                {
                    var access = allAccess.FirstOrDefault(a =>
                        a.RoleId == role.Id &&
                        a.ModuleId == module.Id);

                    matrix[role.Id][module.Id] = access?.CanAccess ?? false;
                }
            }

            return matrix;
        }
    }
}
