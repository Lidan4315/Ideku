using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("Departments")]
    public class Department
    {
        [Key]
        [Column("Id")]
        [StringLength(3)]
        public string Id { get; set; } = string.Empty;

        [Required]
        [Column("NameDepartment")]
        [StringLength(200)]
        public string NameDepartment { get; set; } = string.Empty;

        [Required]
        [Column("DivisiId")]
        [StringLength(3)]
        public string DivisiId { get; set; } = string.Empty;

        [Required]
        [Column("IsActive")]
        public bool IsActive { get; set; } = true;

        [Required]
        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("DivisiId")]
        public Division Division { get; set; } = null!;
        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
        public ICollection<Idea> TargetIdeas { get; set; } = new List<Idea>();
    }
}