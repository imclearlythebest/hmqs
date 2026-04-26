using Hmqs.Api.Dtos;
using Hmqs.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Hmqs.Api.Controllers;

[ApiController]
[Route("api/playlists")]
public class PlaylistController : AuthorizedApiController
{
    private readonly PlaylistService _playlistService;

    public PlaylistController(PlaylistService playlistService)
    {
        _playlistService = playlistService;
    }

    [HttpPost]
    public async Task<ActionResult<PlaylistResponseDto>> Create([FromBody] PlaylistCreateDto model)
    {
        var response = await _playlistService.CreatePlaylistAsync(GetCurrentListenerId(), model);
        return CreatedAtAction(nameof(GetById), new { playlistId = response.Id }, response);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PlaylistResponseDto>>> GetAll()
    {
        var response = await _playlistService.GetUserPlaylistsAsync(GetCurrentListenerId());
        return Ok(response);
    }

    [HttpGet("{playlistId:guid}")]
    public async Task<ActionResult<PlaylistResponseDto>> GetById(Guid playlistId)
    {
        var response = await _playlistService.GetPlaylistAsync(GetCurrentListenerId(), playlistId);
        if (response is null)
        {
            return NotFound();
        }

        return Ok(response);
    }

    [HttpPatch("{playlistId:guid}")]
    public async Task<ActionResult<PlaylistResponseDto>> Update(Guid playlistId, [FromBody] PlaylistUpdateDto model)
    {
        var response = await _playlistService.UpdatePlaylistAsync(GetCurrentListenerId(), playlistId, model);
        return Ok(response);
    }

    [HttpDelete("{playlistId:guid}")]
    public async Task<IActionResult> Delete(Guid playlistId)
    {
        await _playlistService.DeletePlaylistAsync(GetCurrentListenerId(), playlistId);
        return NoContent();
    }

    [HttpPost("{playlistId:guid}/tracks")]
    public async Task<ActionResult<PlaylistTrackResponseDto>> AddTrack(Guid playlistId, [FromBody] PlaylistTrackCreateDto model)
    {
        var response = await _playlistService.AddTrackAsync(GetCurrentListenerId(), playlistId, model);
        return Ok(response);
    }

    [HttpPatch("{playlistId:guid}/tracks/{trackId:int}")]
    public async Task<ActionResult<PlaylistTrackResponseDto>> MoveTrack(Guid playlistId, int trackId, [FromBody] PlaylistTrackMoveDto model)
    {
        var response = await _playlistService.MoveTrackAsync(GetCurrentListenerId(), playlistId, trackId, model.Position);
        return Ok(response);
    }

    [HttpDelete("{playlistId:guid}/tracks/{trackId:int}")]
    public async Task<IActionResult> RemoveTrack(Guid playlistId, int trackId)
    {
        await _playlistService.RemoveTrackAsync(GetCurrentListenerId(), playlistId, trackId);
        return NoContent();
    }
}
