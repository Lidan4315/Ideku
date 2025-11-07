using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("RoleAccessModules")]
    public class RoleAccessModule
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Required]
        [Column("RoleId")]
        public int RoleId { get; set; }

        [Required]
        [Column("ModuleId")]
        public int ModuleId { get; set; }

        [Column("CanAccess")]
        public bool CanAccess { get; set; } = true;

        [Column("ModifiedBy")]
        public long? ModifiedBy { get; set; }

        [Column("ModifiedAt")]
        public DateTime ModifiedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("RoleId")]
        public Role Role { get; set; } = null!;

        [ForeignKey("ModuleId")]
        public Module Module { get; set; } = null!;

        [ForeignKey("ModifiedBy")]
        public User? ModifiedByUser { get; set; }
    }
}
