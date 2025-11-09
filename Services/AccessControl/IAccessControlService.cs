using Ideku.Models.Entities;

namespace Ideku.Services.AccessControl
{
    public interface IAccessControlService
    {
        // AUTHORIZATION CHECKS
        Task<bool> CanAccessModuleAsync(long userId, string moduleKey);
        Task<bool> CanAccessControllerAsync(long userId, string controllerName);
        Task<IEnumerable<Module>> GetUserAccessibleModulesAsync(long userId);

        Task<IEnumerable<Module>> GetUserAccessibleModulesAsync(string username);

        // MODULE MANAGEMENT
        Task<IEnumerable<Module>> GetAllModulesAsync();
        Task<Module?> GetModuleByIdAsync(int id);

        // PERMISSION MANAGEMENT
        Task<Dictionary<int, Dictionary<int, bool>>> GetPermissionMatrixAsync();
        Task<IEnumerable<RoleAccessModule>> GetRoleAccessModulesAsync(int roleId);
        Task<(bool Success, string Message)> UpdateRoleAccessAsync(int roleId, List<int> moduleIds, long modifiedBy);
        Task<(bool Success, string Message)> GrantAccessAsync(int roleId, int moduleId, long modifiedBy);
        Task<(bool Success, string Message)> RevokeAccessAsync(int roleId, int moduleId, long modifiedBy);

        // CACHE MANAGEMENT
        void ClearUserCache(long userId);
        Task ClearRoleCacheAsync(int roleId);
    }
}
