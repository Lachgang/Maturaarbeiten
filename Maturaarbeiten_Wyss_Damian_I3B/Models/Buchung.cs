using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("mat_buchung")]
public class Buchung
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("person_id")]
    public Guid PersonId { get; set; }

    [ForeignKey("PersonId")]
    public Person? Person { get; set; }

    [Column("praesentation_id")]
    public int PraesentationId { get; set; }

    [ForeignKey("PraesentationId")]
    public G4Praesentation? Praesentation { get; set; }

    [Column("startzeit")]
    public DateTime Startzeit { get; set; }
}