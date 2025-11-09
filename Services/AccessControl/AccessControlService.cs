using Microsoft.Extensions.Caching.Memory;
using Ideku.Data.Repositories.AccessControl;
using Ideku.Data.Repositories;
using Ideku.Models.Entities;

namespace Ideku.Services.AccessControl
{
    public class AccessControlService : IAccessControlService
    {
        private readonly IAccessControlRepository _accessControlRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMemoryCache _cache;
        private readonly ILogger<AccessControlService> _logger;

        private const string CACHE_KEY_USER_MODULES = "UserModules_{0}";
        private const int CACHE_DURATION_MINUTES = 30;

        public AccessControlService(
            IAccessControlRepository accessControlRepository,
            IUserRepository userRepository,
            IMemoryCache cache,
            ILogger<AccessControlService> logger)
        {
            _accessControlRepository = accessControlRepository;
            _userRepository = userRepository;
            _cache = cache;
            _logger = logger;
        }

        // =================== AUTHORIZATION CHECKS ===================

        public async Task<bool> CanAccessModuleAsync(long userId, string moduleKey)
        {
            try
            {
                var effectiveRoleId = await GetEffectiveRoleIdAsync(userId);
                if (effectiveRoleId == null) return false;

                // Check cache
                var cacheKey = string.Format(CACHE_KEY_USER_MODULES, userId);
                if (!_cache.TryGetValue(cacheKey, out HashSet<string>? accessibleModules))
                {
                    // Load from database
                    var modules = await _accessControlRepository.GetAccessibleModuleKeysAsync(effectiveRoleId.Value);
                    accessibleModules = new HashSet<string>(modules, StringComparer.OrdinalIgnoreCase);

                    // Cache it
                    _cache.Set(cacheKey, accessibleModules, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
                }

                return accessibleModules?.Contains(moduleKey) ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking module access for user {UserId}, module {ModuleKey}", userId, moduleKey);
                return false;
            }
        }

        public async Task<bool> CanAccessControllerAsync(long userId, string controllerName)
        {
            try
            {
                var effectiveRoleId = await GetEffectiveRoleIdAsync(userId);
                if (effectiveRoleId == null) return false;

                return await _accessControlRepository.HasAccessToControllerAsync(effectiveRoleId.Value, controllerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking controller access for user {UserId}, controller {ControllerName}", userId, controllerName);
                return false;
            }
        }

        public async Task<IEnumerable<Module>> GetUserAccessibleModulesAsync(long userId)
        {
            try
            {
                var effectiveRoleId = await GetEffectiveRoleIdAsync(userId);
                if (effectiveRoleId == null) return Enumerable.Empty<Module>();

                return await _accessControlRepository.GetAccessibleModulesAsync(effectiveRoleId.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting accessible modules for user {UserId}", userId);
                return Enumerable.Empty<Module>();
            }
        }

        public async Task<IEnumerable<Module>> GetUserAccessibleModulesAsync(string username)
        {
            try
            {
                var user = await _userRepository.GetByUsernameAsync(username);
                if (user == null) return Enumerable.Empty<Module>();

                return await GetUserAccessibleModulesAsync(user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting accessible modules for username {Username}", username);
                return Enumerable.Empty<Module>();
            }
        }

        // =================== MODULE MANAGEMENT ===================

        public async Task<IEnumerable<Module>> GetAllModulesAsync()
        {
            return await _accessControlRepository.GetAllModulesAsync();
        }

        public async Task<Module?> GetModuleByIdAsync(int id)
        {
            return await _accessControlRepository.GetModuleByIdAsync(id);
        }

        // =================== PERMISSION MANAGEMENT ===================

        public async Task<Dictionary<int, Dictionary<int, bool>>> GetPermissionMatrixAsync()
        {
            return await _accessControlRepository.GetPermissionMatrixAsync();
        }

        public async Task<IEnumerable<RoleAccessModule>> GetRoleAccessModulesAsync(int roleId)
        {
            return await _accessControlRepository.GetRoleAccessModulesAsync(roleId);
        }

        public async Task<(bool Success, string Message)> UpdateRoleAccessAsync(int roleId, List<int> moduleIds, long modifiedBy)
        {
            try
            {
                await _accessControlRepository.UpdateRoleAccessModulesAsync(roleId, moduleIds, modifiedBy);

                // Clear cache for all users with this role
                await ClearRoleCacheAsync(roleId);

                _logger.LogInformation("Updated access modules for role {RoleId} by user {UserId}", roleId, modifiedBy);

                return (true, "Access permissions updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role access for role {RoleId}", roleId);
                return (false, $"Error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> GrantAccessAsync(int roleId, int moduleId, long modifiedBy)
        {
            try
            {
                await _accessControlRepository.GrantAccessAsync(roleId, moduleId, modifiedBy);

                // Clear cache for all users with this role
                await ClearRoleCacheAsync(roleId);

                _logger.LogInformation("Granted access to module {ModuleId} for role {RoleId} by user {UserId}", moduleId, roleId, modifiedBy);

                return (true, "Access granted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error granting access for role {RoleId}, module {ModuleId}", roleId, moduleId);
                return (false, $"Error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> RevokeAccessAsync(int roleId, int moduleId, long modifiedBy)
        {
            try
            {
                await _accessControlRepository.RevokeAccessAsync(roleId, moduleId, modifiedBy);

                // Clear cache for all users with this role
                await ClearRoleCacheAsync(roleId);

                _logger.LogInformation("Revoked access to module {ModuleId} for role {RoleId} by user {UserId}", moduleId, roleId, modifiedBy);

                return (true, "Access revoked successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking access for role {RoleId}, module {ModuleId}", roleId, moduleId);
                return (false, $"Error: {ex.Message}");
            }
        }

        // =================== CACHE MANAGEMENT ===================

        public void ClearUserCache(long userId)
        {
            var cacheKey = string.Format(CACHE_KEY_USER_MODULES, userId);
            _cache.Remove(cacheKey);
        }

        public async Task ClearRoleCacheAsync(int roleId)
        {
            // Get all users with this role (including acting users)
            var users = await _userRepository.GetUsersByRoleAsync(roleId);

            foreach (var user in users)
            {
                ClearUserCache(user.Id);
            }
        }

        // =================== HELPER METHODS ===================

        private async Task<int?> GetEffectiveRoleIdAsync(long userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return null;

            // Consider acting role if active
            if (user.IsActing && user.CurrentRoleId.HasValue)
            {
                return user.CurrentRoleId.Value;
            }

            return user.RoleId;
        }
    }
}
