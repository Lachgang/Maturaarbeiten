using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Maturaarbeiten_Wyss_Damian_I3B.Data;
using Maturaarbeiten_Wyss_Damian_I3B.Dtos;
using Maturaarbeiten_Wyss_Damian_I3B.Models;

namespace Maturaarbeiten_Wyss_Damian_I3B.Services;

public interface IAdminService
{
    Task<CsvImportResult> ImportG3SchuelerCsvAsync(
        Stream csvStream,
        string defaultPassword,
        CancellationToken ct = default);
    Task<CsvImportResult> ImportG4PraesentationenCsvAsync(
        Stream csvStream,
        CancellationToken ct = default);
    Task<List<RaumAuslastungDto>> GetRaumAuslastungAsync(CancellationToken ct = default);
    Task<(bool Ok, string Message)> SystemResetAsync(string bestaetigung, CancellationToken ct = default);
}

public sealed class AdminService : IAdminService
{
    private const int MaxPlaetzeProPraesentation = 6;

    private readonly MaturaarbeitenDbContext _db;
    private readonly IAuthService _authService;
    private readonly ILogger<AdminService> _logger;

    public AdminService(
        MaturaarbeitenDbContext db,
        IAuthService authService,
        ILogger<AdminService> logger)
    {
        _db = db;
        _authService = authService;
        _logger = logger;
    }

    public async Task<CsvImportResult> ImportG3SchuelerCsvAsync(
        Stream csvStream,
        string defaultPassword,
        CancellationToken ct = default)
    {
        using var reader = new StreamReader(csvStream);
        int imported = 0;
        int skipped = 0;
        var hinweise = new List<string>();
        int lineNumber = 0;

        while ((await reader.ReadLineAsync(ct)) is { } line)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (lineNumber == 1 && line.Contains("email", StringComparison.OrdinalIgnoreCase))
                continue;

            var parts = line.Split(line.Contains(';') ? ';' : ',');
            if (parts.Length < 3)
            {
                skipped++;
                hinweise.Add($"Zeile {lineNumber}: Ungültige Spaltenanzahl.");
                continue;
            }

            var vorname = parts[0].Trim();
            var nachname = parts[1].Trim();
            var email = parts[2].Trim().ToLowerInvariant();
            var passwort = parts.Length >= 4 && !string.IsNullOrWhiteSpace(parts[3])
                ? parts[3].Trim()
                : defaultPassword;

            if (string.IsNullOrWhiteSpace(vorname) || string.IsNullOrWhiteSpace(nachname) || !email.Contains('@'))
            {
                skipped++;
                hinweise.Add($"Zeile {lineNumber}: Pflichtfelder fehlen oder E-Mail ungültig.");
                continue;
            }

            bool exists = await _db.Personen.AnyAsync(p => p.Email.ToLower() == email, ct);
            if (exists)
            {
                skipped++;
                hinweise.Add($"Zeile {lineNumber}: E-Mail {email} existiert bereits.");
                continue;
            }

            _db.Personen.Add(new Person
            {
                Id = Guid.NewGuid(),
                Vorname = vorname.Length > 20 ? vorname[..20] : vorname,
                Nachname = nachname.Length > 20 ? nachname[..20] : nachname,
                Email = email.Length > 50 ? email[..50] : email,
                PasswortHash = _authService.HashPassword(passwort),
                IstAdmin = false
            });
            imported++;
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation(
            "CSV-Import G3-Lernende: {Imported} importiert, {Skipped} uebersprungen.",
            imported,
            skipped);

        return new CsvImportResult(imported, skipped, hinweise);
    }

    public async Task<CsvImportResult> ImportG4PraesentationenCsvAsync(
        Stream csvStream,
        CancellationToken ct = default)
    {
        using var reader = new StreamReader(csvStream);
        int imported = 0;
        int skipped = 0;
        var hinweise = new List<string>();
        int lineNumber = 0;

        var bekannteRaeume = await _db.Raeume.AsNoTracking().Select(r => r.Id).ToListAsync(ct);
        var raumSet = new HashSet<string>(bekannteRaeume, StringComparer.OrdinalIgnoreCase);

        while ((await reader.ReadLineAsync(ct)) is { } line)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (lineNumber == 1 && line.Contains("thema", StringComparison.OrdinalIgnoreCase))
                continue;

            var parts = line.Split(line.Contains(';') ? ';' : ',');
            if (parts.Length < 5)
            {
                skipped++;
                hinweise.Add($"Zeile {lineNumber}: Erwartet werden 5 Spalten (Thema;Datum;StartZeit;EndZeit;RaumId).");
                continue;
            }

            var thema = parts[0].Trim();
            bool datumOk = DateOnly.TryParse(parts[1].Trim(), CultureInfo.InvariantCulture, out var datum);
            bool startOk = TimeOnly.TryParse(parts[2].Trim(), CultureInfo.InvariantCulture, out var startZeit);
            bool endOk = TimeOnly.TryParse(parts[3].Trim(), CultureInfo.InvariantCulture, out var endZeit);
            var raumId = parts[4].Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(thema) || !datumOk || !startOk || !endOk || endZeit <= startZeit)
            {
                skipped++;
                hinweise.Add($"Zeile {lineNumber}: Thema, Datum oder Zeitspanne ungültig.");
                continue;
            }

            if (!raumSet.Contains(raumId))
            {
                skipped++;
                hinweise.Add($"Zeile {lineNumber}: Raum '{raumId}' existiert nicht.");
                continue;
            }

            bool themaExists = await _db.Praesentationen.AnyAsync(p => p.Thema == thema, ct);
            if (themaExists)
            {
                skipped++;
                hinweise.Add($"Zeile {lineNumber}: Thema '{thema}' ist bereits vorhanden.");
                continue;
            }

            _db.Praesentationen.Add(new G4Praesentation
            {
                Thema = thema.Length > 200 ? thema[..200] : thema,
                Datum = datum,
                StartZeit = startZeit,
                EndZeit = endZeit,
                RaumId = raumId
            });
            imported++;
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation(
            "CSV-Import G4-Praesentationen: {Imported} importiert, {Skipped} uebersprungen.",
            imported,
            skipped);

        return new CsvImportResult(imported, skipped, hinweise);
    }

    public async Task<List<RaumAuslastungDto>> GetRaumAuslastungAsync(CancellationToken ct = default)
    {
        var raeume = await _db.Raeume.AsNoTracking().ToListAsync(ct);
        var praesentationen = await _db.Praesentationen.AsNoTracking().ToListAsync(ct);
        var buchungsCounts = await _db.Buchungen
            .AsNoTracking()
            .GroupBy(b => b.PraesentationId)
            .Select(g => new { PraesentationId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PraesentationId, x => x.Count, ct);

        return raeume.Select(r =>
        {
            var raumPr = praesentationen.Where(p => p.RaumId == r.Id).ToList();
            int belegt = raumPr.Sum(p => buchungsCounts.GetValueOrDefault(p.Id, 0));
            int kapazitaet = raumPr.Count * MaxPlaetzeProPraesentation;
            double prozent = kapazitaet == 0 ? 0 : Math.Round((double)belegt / kapazitaet * 100, 1);

            return new RaumAuslastungDto(
                r.Id,
                r.Bezeichnung,
                raumPr.Count,
                belegt,
                kapazitaet,
                prozent
            );
        }).ToList();
    }

    public async Task<(bool Ok, string Message)> SystemResetAsync(
        string bestaetigung,
        CancellationToken ct = default)
    {
        if (!string.Equals(bestaetigung, "RESET", StringComparison.Ordinal))
            return (false, "Zur Bestätigung muss exakt 'RESET' übergeben werden.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            await _db.Buchungen.ExecuteDeleteAsync(ct);
            await _db.Praesentationen.ExecuteDeleteAsync(ct);
            await _db.Personen.Where(p => !p.IstAdmin).ExecuteDeleteAsync(ct);

            await tx.CommitAsync(ct);
            _logger.LogWarning("System-Reset erfolgreich durch Admin ausgefuehrt.");
            return (true, "System wurde zurückgesetzt (Buchungen, Präsentationen und G3-Lernende gelöscht).");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            _logger.LogError(ex, "Fehler beim System-Reset.");
            return (false, "System-Reset fehlgeschlagen.");
        }
    }
}