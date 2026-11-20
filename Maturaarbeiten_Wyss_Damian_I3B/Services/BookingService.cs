using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Maturaarbeiten_Wyss_Damian_I3B.Data;
using Maturaarbeiten_Wyss_Damian_I3B.Dtos;
using Maturaarbeiten_Wyss_Damian_I3B.Models;

namespace Maturaarbeiten_Wyss_Damian_I3B.Services;

public static class BookingErrorCodes
{
    public const string UserNotFound = "BOOKING_USER_NOT_FOUND";
    public const string PresentationNotFound = "BOOKING_PRESENTATION_NOT_FOUND";
    public const string DuplicateBooking = "BOOKING_DUPLICATE";
    public const string LimitReached = "BOOKING_LIMIT_REACHED";
    public const string TimeConflict = "BOOKING_TIME_CONFLICT";
    public const string PresentationFull = "BOOKING_PRESENTATION_FULL";
    public const string BookingNotFound = "BOOKING_NOT_FOUND";
    public const string Forbidden = "BOOKING_FORBIDDEN";
    public const string ValidationFailed = "BOOKING_VALIDATION_FAILED";
    public const string InternalError = "BOOKING_INTERNAL_ERROR";
}

public interface IBookingService
{
    Task<List<PraesentationDto>> GetKatalogAsync(CancellationToken ct = default);
    Task<List<BuchungDto>> GetMeineBuchungenAsync(Guid personId, CancellationToken ct = default);
    Task<(bool Ok, string? ErrorCode, string Message, BuchungDto? Data)> ErstelleBuchungAsync(
        Guid personId,
        int praesentationId,
        CancellationToken ct = default);
    Task<(bool Ok, string? ErrorCode, string Message)> StorniereBuchungAsync(
        Guid personId,
        int buchungId,
        bool isAdmin,
        CancellationToken ct = default);
}

public sealed class BookingService : IBookingService
{
    private const int MaxBuchungenProPerson = 2;
    private const int MaxZusehendeProPraesentation = 6;

    private readonly MaturaarbeitenDbContext _db;
    private readonly IEmailService _emailService;
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        MaturaarbeitenDbContext db,
        IEmailService emailService,
        ILogger<BookingService> logger)
    {
        _db = db;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<List<PraesentationDto>> GetKatalogAsync(CancellationToken ct = default)
    {
        return await _db.Praesentationen
            .AsNoTracking()
            .Include(p => p.Raum)
            .OrderBy(p => p.Datum)
            .ThenBy(p => p.StartZeit)
            .Select(p => new PraesentationDto(
                p.Id,
                p.Thema,
                p.Datum,
                p.StartZeit,
                p.EndZeit,
                p.RaumId,
                p.Raum != null ? p.Raum.Bezeichnung : p.RaumId,
                _db.Buchungen.Count(b => b.PraesentationId == p.Id),
                MaxZusehendeProPraesentation,
                _db.Buchungen.Count(b => b.PraesentationId == p.Id) >= MaxZusehendeProPraesentation
            ))
            .ToListAsync(ct);
    }

    public async Task<List<BuchungDto>> GetMeineBuchungenAsync(Guid personId, CancellationToken ct = default)
    {
        return await _db.Buchungen
            .AsNoTracking()
            .Where(b => b.PersonId == personId)
            .Include(b => b.Praesentation)
                .ThenInclude(p => p!.Raum)
            .OrderBy(b => b.Praesentation!.Datum)
            .ThenBy(b => b.Praesentation!.StartZeit)
            .Select(b => new BuchungDto(
                b.Id,
                b.PraesentationId,
                b.Praesentation!.Thema,
                b.Praesentation.Datum,
                b.Praesentation.StartZeit,
                b.Praesentation.EndZeit,
                b.Praesentation.RaumId,
                b.Praesentation.Raum != null ? b.Praesentation.Raum.Bezeichnung : b.Praesentation.RaumId,
                b.Startzeit
            ))
            .ToListAsync(ct);
    }

    public async Task<(bool Ok, string? ErrorCode, string Message, BuchungDto? Data)> ErstelleBuchungAsync(
        Guid personId,
        int praesentationId,
        CancellationToken ct = default)
    {
        var person = await _db.Personen
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == personId, ct);

        if (person is null)
            return (false, BookingErrorCodes.UserNotFound, "Benutzerkonto konnte nicht verifiziert werden.", null);

        var zielPraesentation = await _db.Praesentationen
            .AsNoTracking()
            .Include(p => p.Raum)
            .FirstOrDefaultAsync(p => p.Id == praesentationId, ct);

        if (zielPraesentation is null)
            return (false, BookingErrorCodes.PresentationNotFound, "Die gewählte Präsentation existiert nicht.", null);

        // Doppelbuchung derselben Praesentation verhindern
        bool schonGebucht = await _db.Buchungen
            .AsNoTracking()
            .AnyAsync(b => b.PersonId == personId && b.PraesentationId == praesentationId, ct);

        if (schonGebucht)
            return (false, BookingErrorCodes.DuplicateBooking, "Sie haben diese Präsentation bereits gebucht.", null);

        // Maximal 2 Buchungen pro G3-Lernende/r
        var eigeneBuchungen = await _db.Buchungen
            .AsNoTracking()
            .Where(b => b.PersonId == personId)
            .Include(b => b.Praesentation)
            .ToListAsync(ct);

        if (eigeneBuchungen.Count >= MaxBuchungenProPerson)
            return (false, BookingErrorCodes.LimitReached, $"Sie haben bereits das Maximum von {MaxBuchungenProPerson} Buchungen erreicht.", null);

        // Zeitueberschneidung am selben Tag pruefen
        bool zeitKonflikt = eigeneBuchungen.Any(b =>
            b.Praesentation != null &&
            b.Praesentation.Datum == zielPraesentation.Datum &&
            zielPraesentation.StartZeit < b.Praesentation.EndZeit &&
            zielPraesentation.EndZeit > b.Praesentation.StartZeit);

        if (zeitKonflikt)
            return (false, BookingErrorCodes.TimeConflict, "Zeitkonflikt: Zu diesem Zeitpunkt ist bereits eine andere Präsentation gebucht.", null);

        // Raumkapazitaet (max. 6 Zusehende) pruefen
        int aktuelleBelegung = await _db.Buchungen
            .AsNoTracking()
            .CountAsync(b => b.PraesentationId == praesentationId, ct);

        if (aktuelleBelegung >= MaxZusehendeProPraesentation)
            return (false, BookingErrorCodes.PresentationFull, "Diese Präsentation ist bereits ausgebucht (max. 6 Zusehende).", null);

        var buchung = new Buchung
        {
            PersonId = personId,
            PraesentationId = praesentationId,
            Startzeit = DateTime.UtcNow
        };

        try
        {
            _db.Buchungen.Add(buchung);
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx)
        {
            // Technische Details nur intern loggen, niemals an den Client geben
            _logger.LogWarning(
                sqlEx,
                "Datenbank-Validierung bei Buchung (User={UserId}, Praesentation={PraesentationId}, SqlError={Number})",
                personId,
                praesentationId,
                sqlEx.Number);

            var (code, userMessage) = MapSqlExceptionToUserError(sqlEx);
            return (false, code, userMessage, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unerwarteter Fehler beim Speichern der Buchung (User={UserId}).", personId);
            return (false, BookingErrorCodes.InternalError, "Die Buchung konnte aufgrund eines technischen Fehlers nicht durchgeführt werden.", null);
        }

        await _emailService.SendeBuchungsbestaetigungAsync(
            person.Email,
            person.Vorname,
            zielPraesentation,
            ct);

        var dto = new BuchungDto(
            buchung.Id,
            zielPraesentation.Id,
            zielPraesentation.Thema,
            zielPraesentation.Datum,
            zielPraesentation.StartZeit,
            zielPraesentation.EndZeit,
            zielPraesentation.RaumId,
            zielPraesentation.Raum?.Bezeichnung ?? zielPraesentation.RaumId,
            buchung.Startzeit
        );

        return (true, null, "Buchung erfolgreich gespeichert.", dto);
    }

    public async Task<(bool Ok, string? ErrorCode, string Message)> StorniereBuchungAsync(
        Guid personId,
        int buchungId,
        bool isAdmin,
        CancellationToken ct = default)
    {
        var buchung = await _db.Buchungen.FirstOrDefaultAsync(b => b.Id == buchungId, ct);
        if (buchung is null)
            return (false, BookingErrorCodes.BookingNotFound, "Die angegebene Buchung wurde nicht gefunden.");

        // OWASP Broken Access Control: Nur eigene Buchung oder Admin
        if (!isAdmin && buchung.PersonId != personId)
        {
            _logger.LogWarning(
                "Unberechtigter Stornierungsversuch von Benutzer {ActorId} auf Buchung {BuchungId}.",
                personId,
                buchungId);
            return (false, BookingErrorCodes.Forbidden, "Sie besitzen keine Berechtigung zum Stornieren dieser Buchung.");
        }

        try
        {
            _db.Buchungen.Remove(buchung);
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Stornieren von Buchung {BuchungId}.", buchungId);
            return (false, BookingErrorCodes.InternalError, "Die Buchung konnte derzeit nicht storniert werden.");
        }

        _logger.LogInformation("Buchung {BuchungId} wurde entfernt.", buchungId);
        return (true, null, "Buchung erfolgreich storniert.");
    }

    private static (string ErrorCode, string Message) MapSqlExceptionToUserError(SqlException sqlEx)
    {
        // Unique Constraint Violation (2601 / 2627) -> Doppelbuchung
        if (sqlEx.Number is 2601 or 2627)
            return (BookingErrorCodes.DuplicateBooking, "Sie haben diese Präsentation bereits gebucht.");

        var msg = sqlEx.Message;

        if (msg.Contains("zu viele Buchungen", StringComparison.OrdinalIgnoreCase))
            return (BookingErrorCodes.LimitReached, $"Sie haben bereits das Maximum von {MaxBuchungenProPerson} Buchungen erreicht.");

        if (msg.Contains("Raum ist Voll", StringComparison.OrdinalIgnoreCase))
            return (BookingErrorCodes.PresentationFull, "Diese Präsentation ist bereits ausgebucht (max. 6 Zusehende).");

        if (msg.Contains("gleichen Zeit", StringComparison.OrdinalIgnoreCase))
            return (BookingErrorCodes.TimeConflict, "Zeitkonflikt: Zu diesem Zeitpunkt ist bereits eine andere Präsentation gebucht.");

        return (BookingErrorCodes.ValidationFailed, "Die Buchung konnte nicht durchgeführt werden. Bitte prüfen Sie Ihre Auswahl.");
    }
}