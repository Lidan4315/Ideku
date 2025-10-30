using System.ComponentModel.DataAnnotations;

namespace Ideku.ViewModels.IdeaMonitoring
{
    public class UpdateCostSavingsViewModel
    {
        [Required]
        public long MonitoringId { get; set; }

        [Range(0, long.MaxValue, ErrorMessage = "Cost Save Plan must be non-negative")]
        [Display(Name = "Cost Save Plan (USD)")]
        public long? CostSavePlan { get; set; }

        [Range(0, long.MaxValue, ErrorMessage = "Cost Save Actual must be non-negative")]
        [Display(Name = "Cost Save Actual (USD)")]
        public long? CostSaveActual { get; set; }
    }
}
