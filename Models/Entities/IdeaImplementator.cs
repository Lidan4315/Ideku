using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("IdeaImplementators")]
    public class IdeaImplementator
    {
        [Key]
        [Column("Id")]
        public long Id { get; set; }

        [Required]
        [Column("IdeaId")]
        public long IdeaId { get; set; }

        [Required]
        [Column("UserId")]
        public long UserId { get; set; }

        [Required]
        [Column("Role")]
        [StringLength(10)]
        public string Role { get; set; } = string.Empty;

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("UpdatedAt")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("IdeaId")]
        public Idea Idea { get; set; } = null!;

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }
}