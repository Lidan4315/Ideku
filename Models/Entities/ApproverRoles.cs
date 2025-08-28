using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("ApproverRoles")]
    public class ApproverRole
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Required]
        [Column("ApproverId")]
        public int ApproverId { get; set; }

        [ForeignKey("ApproverId")]
        public Approver Approver { get; set; } = null!;

        [Required]
        [Column("RoleId")]
        public int RoleId { get; set; }

        [ForeignKey("RoleId")]
        public Role Role { get; set; } = null!;

        [Required]
        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("UpdatedAt")]
        public DateTime? UpdatedAt { get; set; }
    }
}