namespace Ideku.Models.Statistics
{
    public class DashboardData
    {
        // Global Statistics
        public int TotalIdeas { get; set; }
        public int PendingApproval { get; set; }
        public int Approved { get; set; }
        public int Completed { get; set; }
        public int Rejected { get; set; }

        // Financial
        public long TotalSavingCost { get; set; }
        public long ValidatedSavingCost { get; set; }

        // Alerts
        public int UrgentActingExpirations { get; set; }
        public bool HasAlerts => UrgentActingExpirations > 0;
    }
}
