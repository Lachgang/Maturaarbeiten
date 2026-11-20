using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Maturaarbeiten_Wyss_Damian_I3B.Dtos;
using Maturaarbeiten_Wyss_Damian_I3B.Services;

namespace Maturaarbeiten_Wyss_Damian_I3B.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private const long MaxCsvBytes = 5 * 1024 * 1024; // 5 MB Limit

    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpPost("import/g3-schueler")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CsvImportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ImportG3Schueler(
        IFormFile file, [FromQuery] string defaultPassword = "ChangeMe2026!", CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Bitte eine gültige CSV-Datei hochladen." });

        if (file.Length > MaxCsvBytes)
            return BadRequest(new { message = "CSV-Datei überschreitet das Limit von 5 MB." });

        await using var stream = file.OpenReadStream();
        var result = await _adminService.ImportG3SchuelerCsvAsync(stream, defaultPassword, ct);
        return Ok(result);
    }

    [HttpPost("import/praesentationen")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CsvImportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ImportPraesentationen(IFormFile file, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Bitte eine gültige CSV-Datei hochladen." });

        if (file.Length > MaxCsvBytes)
            return BadRequest(new { message = "CSV-Datei überschreitet das Limit von 5 MB." });

        await using var stream = file.OpenReadStream();
        var result = await _adminService.ImportG4PraesentationenCsvAsync(stream, ct);
        return Ok(result);
    }

    [HttpGet("raum-auslastung")]
    [ProducesResponseType(typeof(List<RaumAuslastungDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<RaumAuslastungDto>>> GetRaumAuslastung(CancellationToken ct)
    {
        var berichte = await _adminService.GetRaumAuslastungAsync(ct);
        return Ok(berichte);
    }

    [HttpPost("system-reset")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SystemReset([FromBody] SystemResetRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var (ok, message) = await _adminService.SystemResetAsync(request.Bestaetigung, ct);
        if (!ok)
            return BadRequest(new { message });

        return Ok(new { message });
    }
}