using Ideku.Models.Entities;

namespace Ideku.Data.Repositories.AccessControl
{
    public interface IAccessControlRepository
    {
        Task<IEnumerable<Module>> GetAllModulesAsync();
        Task<Module?> GetModuleByIdAsync(int id);
        Task<Module?> GetModuleByKeyAsync(string moduleKey);
        Task<Module?> GetModuleByControllerAsync(string controllerName);

        // PERMISSION QUERIES
        Task<bool> HasAccessToModuleAsync(int roleId, string moduleKey);
        Task<bool> HasAccessToControllerAsync(int roleId, string controllerName);

        Task<IEnumerable<Module>> GetAccessibleModulesAsync(int roleId);

        Task<IEnumerable<string>> GetAccessibleModuleKeysAsync(int roleId);

        // ROLE ACCESS MODULE MANAGEMENT
        Task<IEnumerable<RoleAccessModule>> GetRoleAccessModulesAsync(int roleId);
        Task<RoleAccessModule?> GetRoleAccessModuleAsync(int roleId, int moduleId);
        Task<RoleAccessModule> GrantAccessAsync(int roleId, int moduleId, long? modifiedBy);
        Task RevokeAccessAsync(int roleId, int moduleId, long? modifiedBy);
        Task UpdateRoleAccessModulesAsync(int roleId, List<int> moduleIds, long? modifiedBy);
        Task<Dictionary<int, Dictionary<int, bool>>> GetPermissionMatrixAsync();
    }
}
