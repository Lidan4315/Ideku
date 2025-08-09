using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("Division")]
    public class Division
    {
        [Key]
        [Column("Id")]
        [StringLength(3)]
        public string Id { get; set; } = string.Empty;

        [Required]
        [Column("NameDivision")]
        [StringLength(200)]
        public string NameDivision { get; set; } = string.Empty;

        [Required]
        [Column("IsActive")]
        public bool IsActive { get; set; } = true;

        [Required]
        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        public ICollection<Department> Departments { get; set; } = new List<Department>();
        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
        public ICollection<Idea> TargetIdeas { get; set; } = new List<Idea>();
    }
}