using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("Roles")]
    public class Role
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Required]
        [Column("RoleName")]
        [StringLength(100)]
        public string RoleName { get; set; } = string.Empty;

        [Column("Desc")]
        [StringLength(100)]
        public string? Desc { get; set; }

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("UpdatedAt")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}