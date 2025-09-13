using System.ComponentModel.DataAnnotations;

namespace Ideku.ViewModels.UserManagement
{
    /// <summary>
    /// ViewModel for creating new user
    /// Contains validation attributes for client and server-side validation
    /// </summary>
    public class CreateUserViewModel
    {
        /// <summary>
        /// Selected employee ID - required field
        /// </summary>
        [Required(ErrorMessage = "Please select an employee.")]
        [Display(Name = "Employee")]
        public string EmployeeId { get; set; } = string.Empty;

        /// <summary>
        /// Username for login - required field with constraints
        /// </summary>
        [Required(ErrorMessage = "Username is required.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 100 characters.")]
        [Display(Name = "Username")]
        [RegularExpression(@"^[a-zA-Z0-9._-]+$", ErrorMessage = "Username can only contain letters, numbers, dots, hyphens, and underscores.")]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Selected role ID - required field
        /// </summary>
        [Required(ErrorMessage = "Please select a role.")]
        [Display(Name = "Role")]
        public int RoleId { get; set; }

        /// <summary>
        /// Whether this user is in an acting position
        /// </summary>
        [Display(Name = "Acting Position")]
        public bool IsActing { get; set; } = false;
    }
}