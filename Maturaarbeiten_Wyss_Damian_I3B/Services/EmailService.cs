using Maturaarbeiten_Wyss_Damian_I3B.Models;

namespace Maturaarbeiten_Wyss_Damian_I3B.Services;

public interface IEmailService
{
    Task SendeBuchungsbestaetigungAsync(
        string empfaengerEmail,
        string vorname,
        G4Praesentation praesentation,
        CancellationToken ct = default);
}

public sealed class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public Task SendeBuchungsbestaetigungAsync(
        string empfaengerEmail,
        string vorname,
        G4Praesentation praesentation,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Buchungsbestaetigung ausgeloest fuer PraesentationId {PraesentationId} am {Datum} ({Start}-{End}) in Raum {RaumId}.",
            praesentation.Id,
            praesentation.Datum,
            praesentation.StartZeit,
            praesentation.EndZeit,
            praesentation.RaumId);

        return Task.CompletedTask;
    }
}