namespace Ideku.Models.Statistics
{
    public class IdeaListItemDto
    {
        public string IdeaNumber { get; set; } = string.Empty;
        public string IdeaStatus { get; set; } = string.Empty;
        public string CurrentStage { get; set; } = string.Empty;
        public DateTime SubmissionDate { get; set; }
        public int LastUpdatedDays { get; set; }
        public string IdeaFlowValidated { get; set; } = string.Empty;
        public string InitiatorBN { get; set; } = string.Empty;
        public string InitiatorName { get; set; } = string.Empty;
        public string InitiatorDivision { get; set; } = string.Empty;
        public string ImplementOnDivision { get; set; } = string.Empty;
        public string ImplementOnDepartment { get; set; } = string.Empty;
        public string IdeaTitle { get; set; } = string.Empty;
    }
}
