using Hmqs.Api.Dtos;
using Hmqs.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Hmqs.Api.Controllers;

[ApiController]
[Route("api/local-tracks")]
[Authorize]
public class LocalTracksController : ControllerBase
{
    private readonly LocalTrackService _localTrackService;

    public LocalTracksController(LocalTrackService localTrackService)
    {
        _localTrackService = localTrackService;
    }

    private Guid GetCurrentListenerId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new InvalidOperationException("Authenticated user id is missing.");
        }

        return Guid.Parse(userId);
    }

    [HttpPost]
    public async Task<ActionResult<LocalTrackResponseDto>> Create(LocalTrackCreateDto model)
    {
        var response = await _localTrackService.CreateTrackAsync(GetCurrentListenerId(), model);
        return CreatedAtAction(nameof(GetById), new { trackId = response.Id }, response);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LocalTrackResponseDto>>> GetAll()
    {
        var response = await _localTrackService.GetUserTracksAsync(GetCurrentListenerId());
        return Ok(response);
    }

    [HttpGet("{trackId:guid}")]
    public async Task<ActionResult<LocalTrackResponseDto>> GetById(Guid trackId)
    {
        var response = await _localTrackService.GetTrackAsync(GetCurrentListenerId(), trackId);
        if (response is null)
        {
            return NotFound();
        }

        return Ok(response);
    }

    [HttpPatch("{trackId:guid}")]
    public async Task<ActionResult<LocalTrackResponseDto>> Update(Guid trackId, LocalTrackUpdateDto model)
    {
        var response = await _localTrackService.UpdateTrackAsync(GetCurrentListenerId(), trackId, model);
        return Ok(response);
    }

    [HttpDelete("{trackId:guid}")]
    public async Task<IActionResult> Delete(Guid trackId)
    {
        await _localTrackService.DeleteTrackAsync(GetCurrentListenerId(), trackId);
        return NoContent();
    }
}
