using System.ComponentModel.DataAnnotations;

namespace Ideku.ViewModels.RoleManagement
{
    /// <summary>
    /// ViewModel for creating new role
    /// Contains validation attributes for client and server-side validation
    /// </summary>
    public class CreateRoleViewModel
    {
        /// <summary>
        /// Role name - required field with length constraint
        /// </summary>
        [Required(ErrorMessage = "Role name is required.")]
        [StringLength(100, ErrorMessage = "Role name cannot exceed 100 characters.")]
        [Display(Name = "Role Name")]
        public string RoleName { get; set; } = string.Empty;

        /// <summary>
        /// Optional description for the role
        /// </summary>
        [StringLength(100, ErrorMessage = "Description cannot exceed 100 characters.")]
        [Display(Name = "Description")]
        public string? Description { get; set; }
    }
}