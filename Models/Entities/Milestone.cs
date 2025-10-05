using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("Milestones")]
    public class Milestone
    {
        [Key]
        [Column("Id")]
        public long Id { get; set; }

        [Required]
        [Column("IdeaId")]
        public long IdeaId { get; set; }

        [Required]
        [Column("CreatorName")]
        [StringLength(100)]
        public string CreatorName { get; set; } = string.Empty;

        [Required]
        [Column("CreatorEmployeeId")]
        [StringLength(20)]
        public string CreatorEmployeeId { get; set; } = string.Empty;

        [Required]
        [Column("TitleMilestone")]
        [StringLength(50)]
        public string TitleMilestone { get; set; } = string.Empty;

        [Required]
        [Column("Status")]
        [StringLength(20)]
        public string Status { get; set; } = string.Empty;

        [Required]
        [Column("Note")]
        public string Note { get; set; } = string.Empty;

        [Required]
        [Column("StartDate")]
        public DateTime StartDate { get; set; }

        [Required]
        [Column("EndDate")]
        public DateTime EndDate { get; set; }

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("UpdatedAt")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("IdeaId")]
        public Idea Idea { get; set; } = null!;

        // Collection Navigation Properties
        public ICollection<MilestonePIC> MilestonePICs { get; set; } = new List<MilestonePIC>();
    }
}