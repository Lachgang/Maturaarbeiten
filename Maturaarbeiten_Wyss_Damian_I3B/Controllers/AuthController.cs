using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Maturaarbeiten_Wyss_Damian_I3B.Dtos;
using Maturaarbeiten_Wyss_Damian_I3B.Services;

namespace Maturaarbeiten_Wyss_Damian_I3B.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(request, ct);
        if (result is null)
            return Unauthorized(new { message = "Ungültige E-Mail-Adresse oder ungültiges Passwort." });

        return Ok(result);
    }

    // TEMPORÄR: Nach dem Generieren des BCrypt-Hashes wieder entfernen
    [HttpGet("dev-hash")]
    public IActionResult GenerateDevHash([FromQuery] string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return BadRequest(new { message = "Bitte ein Passwort angeben." });

        return Ok(new
        {
            password,
            bcryptHash = _authService.HashPassword(password)
        });
    }
}