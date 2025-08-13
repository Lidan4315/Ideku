using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("Workflows")]
    public class Workflow
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Required]
        [Column("WorkflowName")]
        [StringLength(20)]
        public string WorkflowName { get; set; } = string.Empty;

        [Required]
        [Column("Desc")]
        [StringLength(200)]
        public string Desc { get; set; } = string.Empty;

        [Required]
        [Column("IsActive")]
        public bool IsActive { get; set; } = true;

        [Required]
        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("UpdatedAt")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public ICollection<WorkflowStage> WorkflowStages { get; set; } = new List<WorkflowStage>();
        public ICollection<WorkflowCondition> WorkflowConditions { get; set; } = new List<WorkflowCondition>();
    }
}