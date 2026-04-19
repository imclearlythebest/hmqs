using Hmqs.Api.Dtos;
using Hmqs.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Hmqs.Api.Controllers;

[ApiController]
[Route("api/scrobbles")]
[Authorize]
public class ScrobbleController : ControllerBase
{
    private readonly ScrobbleService _scrobbleService;

    public ScrobbleController(ScrobbleService scrobbleService)
    {
        _scrobbleService = scrobbleService;
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

    [HttpPost("submit")]
    public async Task<ActionResult<ScrobbleResponseDto>> Submit(ScrobbleDto model)
    {
        var listenerId = GetCurrentListenerId();
        var response = await _scrobbleService.CreateScrobbleAsync(listenerId, model);
        return Ok(response);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ScrobbleResponseDto>>> GetScrobbles([FromQuery] int limit = 50)
    {
        var listenerId = GetCurrentListenerId();
        var scrobbles = await _scrobbleService.GetScrobblesAsync(listenerId, limit);
        return Ok(scrobbles);
    }
}
