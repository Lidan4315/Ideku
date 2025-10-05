using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("MilestonePICs")]
    public class MilestonePIC
    {
        [Key]
        [Column("Id")]
        public long Id { get; set; }

        [Required]
        [Column("MilestoneId")]
        public long MilestoneId { get; set; }

        [Required]
        [Column("UserId")]
        public long UserId { get; set; }

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("UpdatedAt")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("MilestoneId")]
        public Milestone Milestone { get; set; } = null!;

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }
}