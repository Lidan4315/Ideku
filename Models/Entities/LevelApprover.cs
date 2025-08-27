using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("LevelApprovers")]
    public class LevelApprover
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Required]
        [Column("LevelId")]
        public int LevelId { get; set; }

        [ForeignKey("LevelId")]
        public Level Level { get; set; } = null!;

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