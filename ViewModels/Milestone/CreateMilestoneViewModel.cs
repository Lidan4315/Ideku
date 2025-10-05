using System.ComponentModel.DataAnnotations;

namespace Ideku.ViewModels.Milestone
{
    /// <summary>
    /// ViewModel for creating a new milestone
    /// </summary>
    public class CreateMilestoneViewModel
    {
        /// <summary>
        /// Idea ID this milestone belongs to
        /// </summary>
        public long IdeaId { get; set; }

        /// <summary>
        /// Milestone title
        /// </summary>
        [Required(ErrorMessage = "Milestone title is required.")]
        [StringLength(50, ErrorMessage = "Title cannot exceed 50 characters.")]
        [Display(Name = "Milestone Title")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Milestone description/note
        /// </summary>
        [Required(ErrorMessage = "Milestone description is required.")]
        [Display(Name = "Description")]
        public string Note { get; set; } = string.Empty;

        /// <summary>
        /// Milestone start date
        /// </summary>
        [Required(ErrorMessage = "Start date is required.")]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Today;

        /// <summary>
        /// Milestone end date
        /// </summary>
        [Required(ErrorMessage = "End date is required.")]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; } = DateTime.Today.AddDays(30);

        /// <summary>
        /// Milestone status
        /// </summary>
        [Required(ErrorMessage = "Status is required.")]
        [StringLength(20, ErrorMessage = "Status cannot exceed 20 characters.")]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Planning";

        /// <summary>
        /// Selected PIC user IDs
        /// </summary>
        [Display(Name = "Person In Charge (PIC)")]
        public List<long> SelectedPICUserIds { get; set; } = new List<long>();

        /// <summary>
        /// Available implementators for PIC selection (from idea implementators with roles)
        /// </summary>
        public IEnumerable<ImplementatorForPICDto> AvailablePICUsers { get; set; } = new List<ImplementatorForPICDto>();

        /// <summary>
        /// Creator name (will be set from current user)
        /// </summary>
        public string CreatorName { get; set; } = string.Empty;

        /// <summary>
        /// Creator employee ID (will be set from current user)
        /// </summary>
        public string CreatorEmployeeId { get; set; } = string.Empty;

        /// <summary>
        /// Predefined status options
        /// </summary>
        public static readonly List<string> StatusOptions = new List<string>
        {
            "Planning",
            "In Progress",
            "On Hold",
            "Completed",
            "Cancelled"
        };
    }
}