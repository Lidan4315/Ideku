namespace Ideku.Models.Statistics
{
    /// <summary>
    /// DTO for acting users statistics
    /// Used for dashboard and reporting purposes
    /// </summary>
    public class ActingStatistics
    {
        /// <summary>
        /// Total number of users currently acting
        /// </summary>
        public int TotalActingUsers { get; set; }

        /// <summary>
        /// Number of acting users whose period will expire within 7 days
        /// </summary>
        public int ExpiringIn7Days { get; set; }

        /// <summary>
        /// Number of acting users whose period will expire within 30 days
        /// </summary>
        public int ExpiringIn30Days { get; set; }

        /// <summary>
        /// Number of acting users whose period has already expired
        /// </summary>
        public int ExpiredActingUsers { get; set; }

        /// <summary>
        /// Average acting duration in days across all current acting users
        /// </summary>
        public double AverageActingDurationDays { get; set; }

        /// <summary>
        /// Most common acting role name
        /// </summary>
        public string MostCommonActingRole { get; set; } = string.Empty;

        /// <summary>
        /// Number of users using the most common acting role
        /// </summary>
        public int MostCommonActingRoleCount { get; set; }

        /// <summary>
        /// Total number of regular users (not acting)
        /// </summary>
        public int TotalRegularUsers { get; set; }

        /// <summary>
        /// Percentage of users currently acting
        /// </summary>
        public double ActingPercentage => TotalActingUsers + TotalRegularUsers > 0
            ? (double)TotalActingUsers / (TotalActingUsers + TotalRegularUsers) * 100
            : 0;

        /// <summary>
        /// Check if there are any urgent acting expirations (within 3 days)
        /// </summary>
        public bool HasUrgentExpirations { get; set; }

        /// <summary>
        /// Number of acting users expiring within 3 days
        /// </summary>
        public int UrgentExpirations { get; set; }
    }
}