using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("Categories")]
    public class Category
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Required]
        [Column("CategoryName")]
        [StringLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [Column("Desc")]
        [StringLength(200)]
        public string? Desc { get; set; }

        [Column("IsActive")]
        public bool IsActive { get; set; } = true;

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        public ICollection<Idea> Ideas { get; set; } = new List<Idea>();
    }
}