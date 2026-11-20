using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Maturaarbeiten_Wyss_Damian_I3B.Dtos;
using Maturaarbeiten_Wyss_Damian_I3B.Services;

namespace Maturaarbeiten_Wyss_Damian_I3B.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PraesentationenController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public PraesentationenController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<PraesentationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PraesentationDto>>> GetAll(CancellationToken ct)
    {
        var katalog = await _bookingService.GetKatalogAsync(ct);
        return Ok(katalog);
    }
}