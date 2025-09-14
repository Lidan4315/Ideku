using Ideku.Models.Entities;
using Ideku.Services.Roles;

namespace Ideku.ViewModels.RoleManagement
{
    /// <summary>
    /// ViewModel for Role Management Index page
    /// Contains all data needed for the main role management interface
    /// </summary>
    public class RoleIndexViewModel
    {
        /// <summary>
        /// List of all roles to display in the table
        /// </summary>
        public IEnumerable<Role> Roles { get; set; } = new List<Role>();

        /// <summary>
        /// Form data for creating new role (used in modal)
        /// </summary>
        public CreateRoleViewModel CreateRoleForm { get; set; } = new CreateRoleViewModel();

        /// <summary>
        /// Statistics data for dashboard cards
        /// </summary>
        public RoleStatistics Statistics { get; set; } = new RoleStatistics();
    }
}