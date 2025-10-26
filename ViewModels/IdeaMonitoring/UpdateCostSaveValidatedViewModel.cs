using System.ComponentModel.DataAnnotations;

namespace Ideku.ViewModels.IdeaMonitoring
{
    public class UpdateCostSaveValidatedViewModel
    {
        [Required]
        public long MonitoringId { get; set; }

        [Required(ErrorMessage = "Cost Save Actual Validated is required")]
        [Range(0, long.MaxValue, ErrorMessage = "Cost Save Actual Validated must be non-negative")]
        [Display(Name = "Cost Save Actual Validated (USD)")]
        public long CostSaveActualValidated { get; set; }
    }
}
