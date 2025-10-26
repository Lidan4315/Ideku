using System.ComponentModel.DataAnnotations;

namespace Ideku.ViewModels.IdeaMonitoring
{
    public class CreateMonitoringViewModel
    {
        [Required(ErrorMessage = "Idea ID is required")]
        public long IdeaId { get; set; }

        [Required(ErrorMessage = "Start month is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Start Month")]
        public DateTime MonthFrom { get; set; } = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

        [Required(ErrorMessage = "Duration is required")]
        [Range(1, 24, ErrorMessage = "Duration must be between 1 and 24 months")]
        [Display(Name = "Duration (Months)")]
        public int DurationMonths { get; set; } = 12;

        // Display properties
        public string? IdeaCode { get; set; }
        public string? IdeaName { get; set; }
    }
}
