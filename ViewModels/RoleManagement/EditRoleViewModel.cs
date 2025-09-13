using System.ComponentModel.DataAnnotations;

namespace Ideku.ViewModels.RoleManagement
{
    /// <summary>
    /// ViewModel for editing existing role
    /// Contains ID for identification and form fields for updates
    /// </summary>
    public class EditRoleViewModel
    {
        /// <summary>
        /// Role ID for identification (hidden field in form)
        /// </summary>
        [Required]
        public int Id { get; set; }

        /// <summary>
        /// Updated role name with validation
        /// </summary>
        [Required(ErrorMessage = "Role name is required.")]
        [StringLength(100, ErrorMessage = "Role name cannot exceed 100 characters.")]
        [Display(Name = "Role Name")]
        public string RoleName { get; set; } = string.Empty;

        /// <summary>
        /// Updated description (optional)
        /// </summary>
        [StringLength(100, ErrorMessage = "Description cannot exceed 100 characters.")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        /// <summary>
        /// Number of users assigned to this role (for warning display)
        /// </summary>
        public int UserCount { get; set; }
    }
}