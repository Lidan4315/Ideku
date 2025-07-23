// File: Models/Role.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models // Pastikan namespace sesuai dengan nama proyek Anda
{
    [Table("roles")]
    public class Role
    {
        [Key]
        public int Id { get; set; } // ID integer standar yang akan auto-increment

        [Required]
        [Column(TypeName = "varchar(20)")]// Memetakan properti ke nama kolom yang Anda inginkan
        public string RoleName { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; }
        
        public DateTime? UpdatedAt { get; set; }
    }
}