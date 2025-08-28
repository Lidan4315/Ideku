using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("WorkflowStages")]
    public class WorkflowStage
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Required]
        [Column("WorkflowId")]
        public int WorkflowId { get; set; }

        [ForeignKey("WorkflowId")]
        public Workflow Workflow { get; set; } = null!;

        [Required]
        [Column("ApproverId")]
        public int ApproverId { get; set; }

        [ForeignKey("ApproverId")]
        public Approver Approver { get; set; } = null!;

        [Required]
        [Column("Stage")]
        public int Stage { get; set; }

        [Required]
        [Column("IsMandatory")]
        public bool IsMandatory { get; set; } = true;

        [Required]
        [Column("IsParallel")]
        public bool IsParallel { get; set; } = false;

        [Required]
        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("UpdatedAt")]
        public DateTime? UpdatedAt { get; set; }
    }
}