using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models
{
    [Table("departement")]
    public class Departement
    {
        [Key]
        [Column("id")]
        public string Id { get; set; }

        [Required]
        [Column("nama_departement")]
        public string NamaDepartement { get; set; }

        [Required]
        [Column("divisi_id")]
        public string DivisiId { get; set; }
        [ForeignKey("DivisiId")]
        public Divisi Divisi { get; set; }
    }
}