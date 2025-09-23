using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("Users")]
    public class User
    {
        [Key]
        [Column("Id")]
        public long Id { get; set; }

        [Required]
        [Column("EmployeeId")]
        [StringLength(10)]
        public string EmployeeId { get; set; } = string.Empty;

        [Required]
        [Column("RoleId")]
        public int RoleId { get; set; }

        [Required]
        [Column("Username")]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [Column("Name")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Column("IsActing")]
        public bool IsActing { get; set; } = false;

        [Column("CurrentRoleId")]
        public int? CurrentRoleId { get; set; }

        [Column("ActingStartDate")]
        public DateTime? ActingStartDate { get; set; }

        [Column("ActingEndDate")]
        public DateTime? ActingEndDate { get; set; }

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("UpdatedAt")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("EmployeeId")]
        public Employee Employee { get; set; } = null!;

        [ForeignKey("RoleId")]
        public Role Role { get; set; } = null!;

        [ForeignKey("CurrentRoleId")]
        public Role? CurrentRole { get; set; }

        public ICollection<Idea> InitiatedIdeas { get; set; } = new List<Idea>();
        public ICollection<WorkflowHistory> WorkflowActions { get; set; } = new List<WorkflowHistory>();
        public ICollection<Milestone> CreatedMilestones { get; set; } = new List<Milestone>();

        // =================== ACTING DURATION HELPER METHODS ===================

        /// <summary>
        /// Check if user is currently in active acting period
        /// </summary>
        public bool IsCurrentlyActing()
        {
            if (!IsActing || !ActingStartDate.HasValue || !ActingEndDate.HasValue)
                return false;

            var now = DateTime.Now;
            return now >= ActingStartDate.Value && now < ActingEndDate.Value;
        }

        /// <summary>
        /// Get remaining days in acting period
        /// </summary>
        public int GetActingDaysRemaining()
        {
            if (!IsCurrentlyActing() || !ActingEndDate.HasValue)
                return 0;

            var remainingTime = ActingEndDate.Value - DateTime.Now;
            return Math.Max(0, (int)Math.Ceiling(remainingTime.TotalDays));
        }

        /// <summary>
        /// Get total acting period duration in days
        /// </summary>
        public int GetActingDurationInDays()
        {
            if (!ActingStartDate.HasValue || !ActingEndDate.HasValue)
                return 0;

            return (int)Math.Ceiling((ActingEndDate.Value - ActingStartDate.Value).TotalDays);
        }

        /// <summary>
        /// Get formatted acting status text for UI display
        /// </summary>
        public string GetActingStatusText()
        {
            if (!IsCurrentlyActing())
                return "Regular";

            var daysLeft = GetActingDaysRemaining();
            var endDate = ActingEndDate!.Value.ToString("MMM dd, yyyy");

            if (daysLeft <= 0)
                return $"Acting period ended ({endDate})";

            return $"Acting until {endDate} ({daysLeft} {(daysLeft == 1 ? "day" : "days")} left)";
        }

        /// <summary>
        /// Get role display text with acting indicator
        /// </summary>
        public string GetRoleDisplayText()
        {
            if (IsCurrentlyActing())
            {
                var originalRole = CurrentRole?.RoleName ?? "Unknown";
                return $"{Role.RoleName} (Acting from {originalRole})";
            }
            return Role.RoleName;
        }

        /// <summary>
        /// Check if acting period is about to expire (within specified days)
        /// </summary>
        public bool IsActingExpiringSoon(int withinDays = 7)
        {
            if (!IsCurrentlyActing() || !ActingEndDate.HasValue)
                return false;

            var daysRemaining = GetActingDaysRemaining();
            return daysRemaining <= withinDays && daysRemaining > 0;
        }
    }
}