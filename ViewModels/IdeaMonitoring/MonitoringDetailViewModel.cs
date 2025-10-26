using Ideku.Models.Entities;

namespace Ideku.ViewModels.IdeaMonitoring
{
    public class MonitoringDetailViewModel
    {
        public Idea Idea { get; set; } = null!;
        public IEnumerable<Models.Entities.IdeaMonitoring> Monitorings { get; set; } = new List<Models.Entities.IdeaMonitoring>();

        // Permission flags
        public bool CanEditCostSavings { get; set; } = false;
        public bool CanValidateCostSavings { get; set; } = false;
        public bool HasMonitoring { get; set; } = false;
    }
}
