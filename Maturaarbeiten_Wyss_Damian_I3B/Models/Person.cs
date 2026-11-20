using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("mat_person")]
public class Person
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("vorname")]
    [Required]
    [MaxLength(20)]
    public string Vorname { get; set; } = null!;

    [Column("nachname")]
    [Required]
    [MaxLength(20)]
    public string Nachname { get; set; } = null!;

    [Column("email")]
    [Required]
    [MaxLength(50)]
    public string Email { get; set; } = null!;

    [Column("passwort_hash")]
    [Required]
    [MaxLength(256)]
    public string PasswortHash { get; set; } = null!;

    [Column("ist_Admin")]
    public bool IstAdmin { get; set; }
}