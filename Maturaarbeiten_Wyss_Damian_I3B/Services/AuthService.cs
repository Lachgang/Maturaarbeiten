using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Maturaarbeiten_Wyss_Damian_I3B.Data;
using Maturaarbeiten_Wyss_Damian_I3B.Dtos;

namespace Maturaarbeiten_Wyss_Damian_I3B.Services;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default);
    string HashPassword(string plainPassword);
}

public sealed class AuthService : IAuthService
{
    private readonly MaturaarbeitenDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    public AuthService(MaturaarbeitenDbContext db, IConfiguration config, ILogger<AuthService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    public string HashPassword(string plainPassword)
        => BCrypt.Net.BCrypt.HashPassword(plainPassword, workFactor: 12);

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Passwort))
        {
            _logger.LogWarning("Fehlgeschlagener Login-Versuch mit leeren Zugangsdaten.");
            return null;
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // Parametrisierte EF Core-Abfrage (Schutz vor SQL Injection)
        var person = await _db.Personen
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Email != null && p.Email.ToLower() == normalizedEmail, ct);

        if (person is null || string.IsNullOrWhiteSpace(person.PasswortHash))
        {
            _logger.LogWarning("Fehlgeschlagener Login-Versuch fuer unbekannte E-Mail.");
            return null;
        }

        bool validPassword;
        try
        {
            validPassword = BCrypt.Net.BCrypt.Verify(request.Passwort, person.PasswortHash);
        }
        catch
        {
            validPassword = false;
        }

        if (!validPassword)
        {
            // Keine Passwoerter oder Hashes in Logs schreiben
            _logger.LogWarning("Fehlgeschlagener Login-Versuch fuer BenutzerId {UserId}.", person.Id);
            return null;
        }

        var jwtKey = _config["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key fehlt in der Konfiguration.");
        var issuer = _config["Jwt:Issuer"] ?? "MaturaarbeitenApi";
        var audience = _config["Jwt:Audience"] ?? "MaturaarbeitenClient";
        var expiresAt = DateTime.UtcNow.AddHours(4);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, person.Id.ToString()),
            new(ClaimTypes.NameIdentifier, person.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, person.Email),
            new(ClaimTypes.GivenName, person.Vorname),
            new(ClaimTypes.Surname, person.Nachname),
            new(ClaimTypes.Role, person.IstAdmin ? "Admin" : "G3Schueler")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        _logger.LogInformation("Erfolgreicher Login fuer BenutzerId {UserId} (Admin={IsAdmin}).", person.Id, person.IstAdmin);

        return new AuthResponse(
            Token: tokenString,
            ExpiresAtUtc: expiresAt,
            BenutzerId: person.Id,
            Email: person.Email,
            Vorname: person.Vorname,
            Nachname: person.Nachname,
            IstAdmin: person.IstAdmin
        );
    }
}