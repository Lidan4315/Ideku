namespace Ideku.Models.Statistics
{
    public class ApprovalHistoryItemDto
    {
        public string IdeaNumber { get; set; } = string.Empty;
        public long ApprovalId { get; set; }
        public string IdeaStatus { get; set; } = string.Empty;
        public string CurrentStage { get; set; } = string.Empty;
        public int StageSequence { get; set; }
        public DateTime ApprovalDate { get; set; }
        public string Approver { get; set; } = string.Empty;
        public DateTime? LatestUpdateDate { get; set; }
        public int LastUpdatedDays { get; set; }
        public string ImplementedDivision { get; set; } = string.Empty;
        public string ImplementedDepartment { get; set; } = string.Empty;
    }
}
