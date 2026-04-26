using Hmqs.Api.Dtos;
using Hmqs.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Hmqs.Api.Controllers;

[ApiController]
[Route("api/scrobbles")]
public class ScrobbleController : AuthorizedApiController
{
    private readonly ScrobbleService _scrobbleService;

    public ScrobbleController(ScrobbleService scrobbleService)
    {
        _scrobbleService = scrobbleService;
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
