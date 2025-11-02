namespace Ideku.Models.Statistics
{
    public class IdeaCostSavingDto
    {
        public string IdeaId { get; set; } = string.Empty;
        public long SavingCostValidated { get; set; }
        public string IdeaCategory { get; set; } = string.Empty;
        public string CurrentStage { get; set; } = string.Empty;
        public string IdeaFlowValidated { get; set; } = string.Empty;
    }
}
