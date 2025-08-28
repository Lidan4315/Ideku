using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("Approvers")]
    public class Approver
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Required]
        [Column("ApproverName")]
        [StringLength(10)]
        public string ApproverName { get; set; } = string.Empty;


        [Required]
        [Column("IsActive")]
        public bool IsActive { get; set; } = true;

        [Required]
        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("UpdatedAt")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public ICollection<ApproverRole> ApproverRoles { get; set; } = new List<ApproverRole>();
        public ICollection<WorkflowStage> WorkflowStages { get; set; } = new List<WorkflowStage>();
    }
}