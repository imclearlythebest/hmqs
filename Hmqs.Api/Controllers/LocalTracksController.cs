using Hmqs.Api.Dtos;
using Hmqs.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Hmqs.Api.Controllers;

[ApiController]
[Route("api/local-tracks")]
public class LocalTracksController : AuthorizedApiController
{
    private readonly LocalTrackService _localTrackService;
    private readonly GlobalTrackService _globalTrackService;

    public LocalTracksController(LocalTrackService localTrackService, GlobalTrackService globalTrackService)
    {
        _localTrackService = localTrackService;
        _globalTrackService = globalTrackService;
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

    [HttpGet("{trackId:guid}/catalogue")]
    public async Task<ActionResult<IEnumerable<GlobalTrackMatchDto>>> SearchCatalogue(Guid trackId, [FromQuery] int limit = 5)
    {
        var response = await _globalTrackService.SearchMatchesAsync(GetCurrentListenerId(), trackId, limit);
        return Ok(response);
    }

    [HttpPost("{trackId:guid}/catalogue/select")]
    public async Task<ActionResult<LocalTrackResponseDto>> SelectCatalogue(Guid trackId, GlobalTrackMatchSelectionDto model)
    {
        var response = await _globalTrackService.SelectMatchAsync(GetCurrentListenerId(), trackId, model);
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
