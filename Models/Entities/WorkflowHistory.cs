using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("WorkflowHistory")]
    public class WorkflowHistory
    {
        [Key]
        [Column("Id")]
        public long Id { get; set; }

        [Required]
        [Column("IdeaId")]
        public long IdeaId { get; set; }

        [Required]
        [Column("ActorUserId")]
        public long ActorUserId { get; set; }

        [Required]
        [Column("FromStage")]
        public int FromStage { get; set; }

        [Column("ToStage")]
        public int? ToStage { get; set; }

        [Required]
        [Column("Action")]
        [StringLength(50)]
        public string Action { get; set; } = string.Empty;

        [Column("Comments")]
        [StringLength(1000)]
        public string? Comments { get; set; }

        [Column("Timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("IdeaId")]
        public Idea Idea { get; set; } = null!;

        [ForeignKey("ActorUserId")]
        public User ActorUser { get; set; } = null!;
    }
}