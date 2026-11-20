using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("mat_g4_praesentation")]
public class G4Praesentation
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("thema")]
    [Required]
    [MaxLength(200)]
    public string Thema { get; set; } = null!;

    [Column("datum")]
    public DateOnly Datum { get; set; }

    [Column("startZeit")]
    public TimeOnly StartZeit { get; set; }

    [Column("endZeit")]
    public TimeOnly EndZeit { get; set; }

    [Column("raum_id")]
    [StringLength(3)]
    public string RaumId { get; set; } = null!;

    [ForeignKey("RaumId")]
    public Raum? Raum { get; set; }
}