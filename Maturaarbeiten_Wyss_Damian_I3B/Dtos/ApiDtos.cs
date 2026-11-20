using System.ComponentModel.DataAnnotations;

namespace Maturaarbeiten_Wyss_Damian_I3B.Dtos;

public record LoginRequest(
    [Required, EmailAddress, MaxLength(50)] string Email,
    [Required, MinLength(6), MaxLength(128)] string Passwort
);

public record AuthResponse(
    string Token,
    DateTime ExpiresAtUtc,
    Guid BenutzerId,
    string Email,
    string Vorname,
    string Nachname,
    bool IstAdmin
);

public record PraesentationDto(
    int Id,
    string Thema,
    DateOnly Datum,
    TimeOnly StartZeit,
    TimeOnly EndZeit,
    string RaumId,
    string RaumBezeichnung,
    int BelegtePlaetze,
    int MaxPlaetze,
    bool IstAusgebucht
);

public record BuchungCreateRequest(
    [Range(1, int.MaxValue)] int PraesentationId
);

public record BuchungDto(
    int Id,
    int PraesentationId,
    string Thema,
    DateOnly Datum,
    TimeOnly StartZeit,
    TimeOnly EndZeit,
    string RaumId,
    string RaumBezeichnung,
    DateTime GebuchtAm
);

public record CsvImportResult(
    int ImportierteZeilen,
    int UebersprungeneZeilen,
    List<string> Hinweise
);

public record RaumAuslastungDto(
    string RaumId,
    string Bezeichnung,
    int AnzahlPraesentationen,
    int BelegtePlaetze,
    int Gesamtkapazitaet,
    double AuslastungProzent
);

public record SystemResetRequest(
    [Required] string Bestaetigung
);