using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("Modules")]
    public class Module
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Required]
        [Column("ModuleName")]
        [StringLength(100)]
        public string ModuleName { get; set; } = string.Empty;

        [Required]
        [Column("ModuleKey")]
        [StringLength(50)]
        public string ModuleKey { get; set; } = string.Empty;

        [Required]
        [Column("ControllerName")]
        [StringLength(100)]
        public string ControllerName { get; set; } = string.Empty;

        [Required]
        [Column("ActionName")]
        [StringLength(100)]
        public string ActionName { get; set; } = "Index";

        [Column("SortOrder")]
        public int SortOrder { get; set; } = 0;

        [Column("IsActive")]
        public bool IsActive { get; set; } = true;

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        public ICollection<RoleAccessModule> RoleAccessModules { get; set; }
            = new List<RoleAccessModule>();
    }
}
