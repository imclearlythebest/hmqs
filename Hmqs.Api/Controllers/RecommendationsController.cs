using Hmqs.Api.Dtos;
using Hmqs.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Hmqs.Api.Controllers;

[ApiController]
[Route("api/recommendations")]
public class RecommendationsController : ControllerBase
{
    private readonly RecommendationService _recommendationService;

    public RecommendationsController(RecommendationService recommendationService)
    {
        _recommendationService = recommendationService;
    }

    [HttpGet("feed")]
    public async Task<ActionResult<RecommendationBatchDto>> Feed([FromQuery] int page = 1, CancellationToken cancellationToken = default)
    {
        var result = await _recommendationService.GetFeedAsync(page, cancellationToken);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("for-you")]
    public async Task<ActionResult<RecommendationBatchDto>> ForYou([FromQuery] int page = 1, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var listenerId))
        {
            return Unauthorized();
        }

        var result = await _recommendationService.GetForYouFeedAsync(listenerId, page, cancellationToken);
        return Ok(result);
    }
}