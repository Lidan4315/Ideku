using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("EMPLIST")]
    public class Employee
    {
        [Key]
        [Column("EMP_ID")]
        [StringLength(10)]
        public string EMP_ID { get; set; } = string.Empty;

        [Required]
        [Column("NAME")]
        [StringLength(200)]
        public string NAME { get; set; } = string.Empty;

        [Required]
        [Column("POSITION_TITLE")]
        [StringLength(200)]
        public string POSITION_TITLE { get; set; } = string.Empty;

        [Required]
        [Column("DIVISION")]
        [StringLength(10)]
        public string DIVISION { get; set; } = string.Empty;

        [Required]
        [Column("DEPARTEMENT")]
        [StringLength(10)]
        public string DEPARTEMENT { get; set; } = string.Empty;

        [Required]
        [Column("EMAIL")]
        [StringLength(200)]
        public string EMAIL { get; set; } = string.Empty;

        [Required]
        [Column("POSITION_LVL")]
        [StringLength(20)]
        public string POSITION_LVL { get; set; } = string.Empty;

        [Column("EMP_STATUS")]
        [StringLength(20)]
        public string EMP_STATUS { get; set; } = "Active";

        // Navigation Properties
        [ForeignKey("DIVISION")]
        public Division DivisionNavigation { get; set; } = null!;

        [ForeignKey("DEPARTEMENT")]
        public Department DepartmentNavigation { get; set; } = null!;

        public User? User { get; set; }
    }
}