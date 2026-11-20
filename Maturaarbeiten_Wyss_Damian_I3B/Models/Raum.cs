using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("mat_raum")]
public class Raum
{
    [Key]
    [Column("id")]
    [StringLength(3)]
    public string Id { get; set; } = null!;

    [Column("bezeichnung")]
    [Required]
    [MaxLength(50)]
    public string Bezeichnung { get; set; } = null!;
}