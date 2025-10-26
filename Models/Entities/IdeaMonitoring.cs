using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("IdeaMonitorings")]
    public class IdeaMonitoring
    {
        [Key]
        [Column("Id")]
        public long Id { get; set; }

        [Required]
        [Column("IdeaId")]
        public long IdeaId { get; set; }

        [Column("MonitoringName")]
        [StringLength(200)]
        public string? MonitoringName { get; set; }

        [Required]
        [Column("MonthFrom")]
        public DateTime MonthFrom { get; set; }

        [Required]
        [Column("MonthTo")]
        public DateTime MonthTo { get; set; }

        [Required]
        [Column("CostSavePlan")]
        public long CostSavePlan { get; set; } = 0;

        [Column("CostSaveActual")]
        public long? CostSaveActual { get; set; }

        [Column("CostSaveActualValidated")]
        public long? CostSaveActualValidated { get; set; }

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("UpdatedAt")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("IdeaId")]
        public Idea Idea { get; set; } = null!;
    }
}
