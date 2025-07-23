// File: Models/Employee.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models // Pastikan namespace sesuai dengan nama proyek Anda
{
    [Table("employees")] // Menentukan nama tabel di database secara eksplisit
    public class Employee
    {
        [Key] // Menandakan ini adalah Primary Key
        [Column(TypeName = "varchar(10)")] // Menentukan tipe data persis di SQL Server
        [DatabaseGenerated(DatabaseGeneratedOption.None)] // Memberitahu EF Core bahwa ID tidak dibuat otomatis oleh database
        public string Id { get; set; }

        [Required]
        [Column(TypeName = "varchar(50)")]
        public string Name { get; set; }

        [Required]
        [Column(TypeName = "varchar(100)")]
        public string? PositionTitle { get; set; }

        [Required]
        [Column(TypeName = "varchar(50)")]
        public string? Division { get; set; }

        [Required]
        [Column(TypeName = "varchar(50)")]
        public string? Department { get; set; }

        [Required]
        [Column(TypeName = "varchar(100)")]
        [EmailAddress]
        public string Email { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string? Position_Lvl { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string? Emp_Status { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string? LdapUser { get; set; }
    }
}