using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Maturaarbeiten_Wyss_Damian_I3B.Dtos;
using Maturaarbeiten_Wyss_Damian_I3B.Services;

namespace Maturaarbeiten_Wyss_Damian_I3B.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BuchungenController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BuchungenController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    private bool TryGetUserId(out Guid userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out userId);
    }

    [HttpGet("meine")]
    [ProducesResponseType(typeof(List<BuchungDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMeine(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(new { errorCode = "AUTH_UNAUTHORIZED", message = "Bitte melden Sie sich erneut an." });

        var buchungen = await _bookingService.GetMeineBuchungenAsync(userId, ct);
        return Ok(buchungen);
    }

    [HttpPost]
    [ProducesResponseType(typeof(BuchungDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Erstellen([FromBody] BuchungCreateRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { errorCode = "VALIDATION_INVALID_INPUT", message = "Ungültige Eingabedaten." });

        if (!TryGetUserId(out var userId))
            return Unauthorized(new { errorCode = "AUTH_UNAUTHORIZED", message = "Bitte melden Sie sich erneut an." });

        var (ok, errorCode, message, data) = await _bookingService.ErstelleBuchungAsync(userId, request.PraesentationId, ct);
        if (!ok || data is null)
        {
            if (errorCode == BookingErrorCodes.InternalError)
                return StatusCode(StatusCodes.Status500InternalServerError, new { errorCode, message });

            return BadRequest(new { errorCode, message });
        }

        return CreatedAtAction(nameof(GetMeine), new { id = data.Id }, data);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Stornieren(int id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(new { errorCode = "AUTH_UNAUTHORIZED", message = "Bitte melden Sie sich erneut an." });

        bool isAdmin = User.IsInRole("Admin");
        var (ok, errorCode, message) = await _bookingService.StorniereBuchungAsync(userId, id, isAdmin, ct);

        if (!ok)
        {
            return errorCode switch
            {
                BookingErrorCodes.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { errorCode, message }),
                BookingErrorCodes.BookingNotFound => NotFound(new { errorCode, message }),
                BookingErrorCodes.InternalError => StatusCode(StatusCodes.Status500InternalServerError, new { errorCode, message }),
                _ => BadRequest(new { errorCode, message })
            };
        }

        return Ok(new { message });
    }
}