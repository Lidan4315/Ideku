using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("Users")]
    public class User
    {
        [Key]
        [Column("Id")]
        public long Id { get; set; }

        [Required]
        [Column("EmployeeId")]
        [StringLength(10)]
        public string EmployeeId { get; set; } = string.Empty;

        [Required]
        [Column("RoleId")]
        public int RoleId { get; set; }

        [Required]
        [Column("Username")]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [Column("Name")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Column("IsActing")]
        public bool IsActing { get; set; } = false;

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("UpdatedAt")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("EmployeeId")]
        public Employee Employee { get; set; } = null!;

        [ForeignKey("RoleId")]
        public Role Role { get; set; } = null!;

        public ICollection<Idea> InitiatedIdeas { get; set; } = new List<Idea>();
        public ICollection<WorkflowHistory> WorkflowActions { get; set; } = new List<WorkflowHistory>();
        public ICollection<Milestone> CreatedMilestones { get; set; } = new List<Milestone>();
    }
}