using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("Levels")]
    public class Level
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Required]
        [Column("Level")]
        [StringLength(10)]
        public string LevelName { get; set; } = string.Empty;


        [Required]
        [Column("IsActive")]
        public bool IsActive { get; set; } = true;

        [Required]
        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("UpdatedAt")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public ICollection<LevelApprover> LevelApprovers { get; set; } = new List<LevelApprover>();
        public ICollection<WorkflowStage> WorkflowStages { get; set; } = new List<WorkflowStage>();
    }
}