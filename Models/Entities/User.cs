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

        [Column("CurrentRoleId")]
        public int? CurrentRoleId { get; set; }

        [Column("ActingStartDate")]
        public DateTime? ActingStartDate { get; set; }

        [Column("ActingEndDate")]
        public DateTime? ActingEndDate { get; set; }

        /// <summary>
        /// Acting Division ID - override Employee division when acting
        /// NULL = use Employee.DIVISION
        /// </summary>
        [Column("ActingDivisionId")]
        [StringLength(3)]
        public string? ActingDivisionId { get; set; }

        /// <summary>
        /// Acting Department ID - override Employee department when acting
        /// NULL = use Employee.DEPARTEMENT
        /// </summary>
        [Column("ActingDepartmentId")]
        [StringLength(3)]
        public string? ActingDepartmentId { get; set; }

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("UpdatedAt")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("EmployeeId")]
        public Employee Employee { get; set; } = null!;

        [ForeignKey("RoleId")]
        public Role Role { get; set; } = null!;

        [ForeignKey("CurrentRoleId")]
        public Role? CurrentRole { get; set; }

        [ForeignKey("ActingDivisionId")]
        public Division? ActingDivision { get; set; }

        [ForeignKey("ActingDepartmentId")]
        public Department? ActingDepartment { get; set; }

        public ICollection<Idea> InitiatedIdeas { get; set; } = new List<Idea>();
        public ICollection<WorkflowHistory> WorkflowActions { get; set; } = new List<WorkflowHistory>();
        public ICollection<IdeaImplementator> ImplementatorAssignments { get; set; } = new List<IdeaImplementator>();
        public ICollection<MilestonePIC> MilestonePICAssignments { get; set; } = new List<MilestonePIC>();
    }
}