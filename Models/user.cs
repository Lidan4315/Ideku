// File: Models/User.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models // Pastikan namespace sesuai
{
    [Table("users")]
    public class User
    {
        [Key]
        public int Id { get; set; } // Primary Key integer auto-increment

        // --- Foreign Key untuk Employee ---
        [Required]
        [Column("employee_id", TypeName = "varchar(10)")]
        public string EmployeeId { get; set; }
        [ForeignKey("EmployeeId")]
        public Employee Employee { get; set; }

        // --- Foreign Key untuk Role ---
        [Required]
        [Column("role_id")]
        public int RoleId { get; set; }
        [ForeignKey("RoleId")]
        public Role Role { get; set; }

        [Required]
        [Column(TypeName = "varchar(50)")]
        public string Username { get; set; }

        [Required]
        [Column(TypeName = "varchar(150)")]
        public string Name { get; set; }

        [Required]
        [Column(TypeName = "varchar(50)")]
        public bool FlagActing { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}