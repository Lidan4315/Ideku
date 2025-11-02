namespace Ideku.Models.Statistics
{
    public class WLChartData
    {
        public long UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string EmployeeId { get; set; } = string.Empty;
        public string Division { get; set; } = string.Empty;
        public string DepartmentId { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public Dictionary<string, int> IdeasByStage { get; set; } = new();
        public int TotalIdeas { get; set; }
    }
}
