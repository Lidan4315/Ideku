using Ideku.Models.Entities;

namespace Ideku.ViewModels
{
    public class AccessControlViewModel
    {
        public List<Role> Roles { get; set; } = new List<Role>();
        public List<Module> Modules { get; set; } = new List<Module>();

        /// Permission Matrix: [RoleId][ModuleId] = CanAccess
        public Dictionary<int, Dictionary<int, bool>> PermissionMatrix { get; set; }
            = new Dictionary<int, Dictionary<int, bool>>();
    }

    public class UpdateRoleAccessRequest
    {
        public int RoleId { get; set; }
        public List<int> ModuleIds { get; set; } = new List<int>();
    }
}
