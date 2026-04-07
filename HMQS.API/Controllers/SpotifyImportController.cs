using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using HMQS.API.DTOs;
using HMQS.API.Services;

namespace HMQS.API.Controllers
{
    [ApiController]
    [Route("api/spotify")]
    [Authorize]
    public class SpotifyImportController : ControllerBase
    {
        private readonly SpotifyImportService _spotifyImportService;

        public SpotifyImportController(SpotifyImportService spotifyImportService)
        {
            _spotifyImportService = spotifyImportService;
        }

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                     ?? User.FindFirst("sub");
            return int.Parse(claim!.Value);
        }

        // POST api/spotify/import
        // Accepts a list of track names and tries to match them
        [HttpPost("import")]
        public async Task<IActionResult> Import([FromBody] SpotifyImportRequestDto request)
        {
            if (request.TrackNames == null || request.TrackNames.Count == 0)
                return BadRequest(new { message = "No tracks provided." });

            if (request.TrackNames.Count > 500)
                return BadRequest(new { message = "Maximum 500 tracks per import." });

            var userId = GetUserId();
            var summary = await _spotifyImportService.ImportAsync(userId, request);
            return Ok(summary);
        }

        // GET api/spotify/imports
        // Returns all past import records for the user
        [HttpGet("imports")]
        public async Task<IActionResult> GetImports()
        {
            var userId = GetUserId();
            var imports = await _spotifyImportService.GetImportsAsync(userId);
            return Ok(imports);
        }

        // GET api/spotify/count
        // Returns total imported track count for the dashboard stat
        [HttpGet("count")]
        public async Task<IActionResult> GetCount()
        {
            var userId = GetUserId();
            var count = await _spotifyImportService.GetImportCountAsync(userId);
            return Ok(new { count });
        }
    }
}