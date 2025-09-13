using Ideku.Models.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ideku.ViewModels.UserManagement
{
    /// <summary>
    /// ViewModel for User Management Index page
    /// Contains all data needed for the main user management interface
    /// </summary>
    public class UserIndexViewModel
    {
        /// <summary>
        /// List of all users to display in the table
        /// </summary>
        public IEnumerable<User> Users { get; set; } = new List<User>();


        /// <summary>
        /// Form data for creating new user (used in modal)
        /// </summary>
        public CreateUserViewModel CreateUserForm { get; set; } = new CreateUserViewModel();

        /// <summary>
        /// Available employees for Create User dropdown
        /// Only employees without user accounts
        /// </summary>
        public IEnumerable<SelectListItem> AvailableEmployees { get; set; } = new List<SelectListItem>();

        /// <summary>
        /// Available roles for user assignment
        /// </summary>
        public IEnumerable<SelectListItem> AvailableRoles { get; set; } = new List<SelectListItem>();
    }
}