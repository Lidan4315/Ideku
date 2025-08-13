using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("WorkflowConditions")]
    public class WorkflowCondition
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
        [Column("ConditionType")]
        [StringLength(50)]
        public string ConditionType { get; set; } = string.Empty; // SAVING_COST, CATEGORY, DIVISION, DEPARTMENT, EVENT

        [Required]
        [Column("Operator")]
        [StringLength(10)]
        public string Operator { get; set; } = string.Empty; // >=, <=, =, !=, IN, NOT_IN

        [Required]
        [Column("ConditionValue")]
        [StringLength(500)]
        public string ConditionValue { get; set; } = string.Empty;

        [Required]
        [Column("IsActive")]
        public bool IsActive { get; set; } = true;

        [Required]
        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("UpdatedAt")]
        public DateTime? UpdatedAt { get; set; }
    }
}