using System.ComponentModel.DataAnnotations;

namespace Ideku.ViewModels.IdeaMonitoring
{
    public class AddKpiViewModel
    {
        [Required(ErrorMessage = "Idea ID is required")]
        public long IdeaId { get; set; }

        [Required(ErrorMessage = "KPI Name is required")]
        [StringLength(200, ErrorMessage = "KPI Name cannot exceed 200 characters")]
        [Display(Name = "KPI Name")]
        public string KpiName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Measurement Unit is required")]
        [StringLength(20, ErrorMessage = "Measurement Unit cannot exceed 20 characters")]
        [Display(Name = "Measurement Unit")]
        public string MeasurementUnit { get; set; } = string.Empty;
    }
}
