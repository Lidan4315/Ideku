using Ideku.Models.Entities;
using Ideku.ViewModels.Common;
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
        /// Paginated users for listing - same pattern as IdeaListViewModel
        /// </summary>
        public PagedResult<User> PagedUsers { get; set; } = new PagedResult<User>();


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
        
        // =================== FILTER PROPERTIES (same pattern as IdeaListViewModel) ===================
        
        /// <summary>
        /// Search term for filtering users by Username, Employee Name, or Employee ID
        /// Preserved across pagination and form submissions
        /// </summary>
        public string? SearchTerm { get; set; }
        
        /// <summary>
        /// Selected role ID for filtering users by role
        /// Preserved across pagination and form submissions
        /// </summary>
        public int? SelectedRole { get; set; }
        
        // Convenience Properties for backward compatibility and ease of use (same as IdeaListViewModel)
        /// <summary>
        /// Users for current page (shortcut to PagedUsers.Items)
        /// </summary>
        public IEnumerable<User> Users => PagedUsers.Items;
        
        /// <summary>
        /// Current page number
        /// </summary>
        public int CurrentPage => PagedUsers.Page;
        
        /// <summary>
        /// Items per page
        /// </summary>
        public int PageSize => PagedUsers.PageSize;
        
        /// <summary>
        /// Total number of users matching current filters
        /// </summary>
        public int TotalItems => PagedUsers.TotalCount;
        
        /// <summary>
        /// Total number of pages
        /// </summary>
        public int TotalPages => PagedUsers.TotalPages;
        
        /// <summary>
        /// Whether there are any users to display
        /// </summary>
        public bool HasUsers => PagedUsers.HasItems;
        
        /// <summary>
        /// Whether to show pagination controls
        /// </summary>
        public bool ShowPagination => PagedUsers.ShowPagination;
    }
}