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
        [Column("CreatorUserId")]
        public long CreatorUserId { get; set; }

        [Required]
        [Column("TitleMilestone")]
        [StringLength(50)]
        public string TitleMilestone { get; set; } = string.Empty;

        [Required]
        [Column("PIC")]
        [StringLength(200)]
        public string PIC { get; set; } = string.Empty;

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

        // Navigation Properties
        [ForeignKey("IdeaId")]
        public Idea Idea { get; set; } = null!;

        [ForeignKey("CreatorUserId")]
        public User CreatorUser { get; set; } = null!;
    }
}