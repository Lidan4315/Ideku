using System.ComponentModel.DataAnnotations;

namespace Ideku.ViewModels.UserManagement
{
    /// <summary>
    /// ViewModel for setting user acting duration
    /// Used for dedicated acting management operations
    /// </summary>
    public class SetActingViewModel
    {
        /// <summary>
        /// User ID to set acting for
        /// </summary>
        [Required]
        public long UserId { get; set; }

        /// <summary>
        /// Acting role ID - the role user will act as
        /// Must be different from current role
        /// </summary>
        [Required(ErrorMessage = "Please select an acting role.")]
        [Display(Name = "Acting Role")]
        public int ActingRoleId { get; set; }

        /// <summary>
        /// When acting period starts
        /// </summary>
        [Required(ErrorMessage = "Acting start date is required.")]
        [Display(Name = "Start Date")]
        public DateTime ActingStartDate { get; set; } = DateTime.Today;

        /// <summary>
        /// When acting period ends
        /// </summary>
        [Required(ErrorMessage = "Acting end date is required.")]
        [Display(Name = "End Date")]
        public DateTime ActingEndDate { get; set; } = DateTime.Today.AddDays(30);

        /// <summary>
        /// Acting Division ID - where user will act (required)
        /// User must explicitly choose acting location
        /// </summary>
        [Required(ErrorMessage = "Please select an acting division.")]
        [Display(Name = "Acting Division")]
        public string ActingDivisionId { get; set; } = string.Empty;

        /// <summary>
        /// Acting Department ID - where user will act (required)
        /// Must belong to selected acting division
        /// </summary>
        [Required(ErrorMessage = "Please select an acting department.")]
        [Display(Name = "Acting Department")]
        public string ActingDepartmentId { get; set; } = string.Empty;

        /// <summary>
        /// Current user information (read-only)
        /// </summary>
        public string UserName { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string CurrentRoleName { get; set; } = string.Empty;
        public string EmployeePosition { get; set; } = string.Empty;
        public string Division { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;

        /// <summary>
        /// Validate that acting period is valid
        /// </summary>
        public bool IsValidActingPeriod()
        {
            return ActingEndDate > ActingStartDate &&
                   ActingStartDate >= DateTime.Today &&
                   ActingEndDate <= DateTime.Today.AddYears(1); // Max 1 year acting period
        }

        /// <summary>
        /// Get acting duration in days
        /// </summary>
        public int GetActingDurationDays()
        {
            return (int)(ActingEndDate - ActingStartDate).TotalDays;
        }

        /// <summary>
        /// Validate acting location selection
        /// Both division and department are now required
        /// </summary>
        public bool IsValidActingLocation()
        {
            // Both division and department must be selected (required fields)
            return !string.IsNullOrEmpty(ActingDivisionId) &&
                   !string.IsNullOrEmpty(ActingDepartmentId);
        }

        /// <summary>
        /// Check if acting location is different from current location
        /// Useful for UI display purposes
        /// </summary>
        public bool HasDifferentActingLocation(string currentDivisionId, string currentDepartmentId) =>
            ActingDivisionId != currentDivisionId || ActingDepartmentId != currentDepartmentId;
    }

    /// <summary>
    /// ViewModel for extending acting period
    /// </summary>
    public class ExtendActingViewModel
    {
        /// <summary>
        /// User ID whose acting period to extend
        /// </summary>
        [Required]
        public long UserId { get; set; }

        /// <summary>
        /// New end date for acting period
        /// Must be after current end date
        /// </summary>
        [Required(ErrorMessage = "New end date is required.")]
        [Display(Name = "New End Date")]
        public DateTime NewActingEndDate { get; set; } = DateTime.Today.AddDays(30);

        /// <summary>
        /// Current acting information (read-only)
        /// </summary>
        public string UserName { get; set; } = string.Empty;
        public string ActingRoleName { get; set; } = string.Empty;
        public DateTime CurrentActingEndDate { get; set; }
        public int CurrentDaysRemaining { get; set; }

        /// <summary>
        /// Get extension duration in days
        /// </summary>
        public int GetExtensionDays()
        {
            return (int)(NewActingEndDate - CurrentActingEndDate).TotalDays;
        }
    }

    /// <summary>
    /// ViewModel for stopping user acting
    /// </summary>
    public class StopActingViewModel
    {
        /// <summary>
        /// User ID whose acting to stop
        /// </summary>
        [Required]
        public long UserId { get; set; }
    }
}