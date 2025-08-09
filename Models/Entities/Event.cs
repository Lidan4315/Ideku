using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("Event")]
    public class Event
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Required]
        [Column("EventName")]
        [StringLength(100)]
        public string EventName { get; set; } = string.Empty;

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