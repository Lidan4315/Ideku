using System.ComponentModel.DataAnnotations;

namespace Ideku.ViewModels.UserManagement
{
    /// <summary>
    /// ViewModel for editing existing user
    /// Contains ID for identification and editable form fields
    /// Employee information is read-only (business rule)
    /// </summary>
    public class EditUserViewModel
    {
        /// <summary>
        /// User ID for identification (hidden field in form)
        /// </summary>
        [Required]
        public long Id { get; set; }

        /// <summary>
        /// Updated username with validation
        /// </summary>
        [Required(ErrorMessage = "Username is required.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 100 characters.")]
        [Display(Name = "Username")]
        [RegularExpression(@"^[a-zA-Z0-9._-]+$", ErrorMessage = "Username can only contain letters, numbers, dots, hyphens, and underscores.")]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Updated role ID
        /// </summary>
        [Required(ErrorMessage = "Please select a role.")]
        [Display(Name = "Role")]
        public int RoleId { get; set; }

        /// <summary>
        /// Updated acting status
        /// </summary>
        [Display(Name = "Acting Position")]
        public bool IsActing { get; set; }

        /// <summary>
        /// Acting start date - required when IsActing is true
        /// </summary>
        [Display(Name = "Acting Start Date")]
        public DateTime? ActingStartDate { get; set; }

        /// <summary>
        /// Acting end date - required when IsActing is true
        /// </summary>
        [Display(Name = "Acting End Date")]
        public DateTime? ActingEndDate { get; set; }

        /// <summary>
        /// Acting role ID - required when IsActing is true
        /// This becomes the active role during acting period
        /// </summary>
        [Display(Name = "Acting Role")]
        public int? ActingRoleId { get; set; }

        // =================== READ-ONLY EMPLOYEE INFORMATION ===================
        // These fields are populated for display but not editable (business rule)

        /// <summary>
        /// Employee ID (read-only display)
        /// </summary>
        public string EmployeeId { get; set; } = string.Empty;

        /// <summary>
        /// Employee full name (read-only display)
        /// </summary>
        public string EmployeeName { get; set; } = string.Empty;

        /// <summary>
        /// Employee position title (read-only display)
        /// </summary>
        public string EmployeePosition { get; set; } = string.Empty;

        /// <summary>
        /// Employee email (read-only display)
        /// </summary>
        public string EmployeeEmail { get; set; } = string.Empty;

        /// <summary>
        /// Division name (read-only display)
        /// </summary>
        public string DivisionName { get; set; } = string.Empty;

        /// <summary>
        /// Department name (read-only display)
        /// </summary>
        public string DepartmentName { get; set; } = string.Empty;

        /// <summary>
        /// Current role name (for display)
        /// </summary>
        public string CurrentRoleName { get; set; } = string.Empty;

        /// <summary>
        /// Count of other users with the same role (for warning display)
        /// </summary>
        public int RoleUsageCount { get; set; }
    }
}