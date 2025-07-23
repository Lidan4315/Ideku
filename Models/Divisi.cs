using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models
{
    [Table("divisi")]
    public class Divisi
    {
        [Key]
        [Column("id")]
        public string Id { get; set; }

        [Required]
        [Column("nama_divisi")]
        public string NamaDivisi { get; set; }
    }
}