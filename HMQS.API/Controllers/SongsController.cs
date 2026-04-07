using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using HMQS.API.DTOs;
using HMQS.API.Services;

namespace HMQS.API.Controllers
{
    [ApiController]
    [Route("api/songs")]
    [Authorize] // Every endpoint here requires a valid JWT token
    public class SongsController : ControllerBase
    {
        private readonly SongService _songService;

        public SongsController(SongService songService)
        {
            _songService = songService;
        }

        // Helper: reads the user ID from the JWT token claims
        // When the user logs in, we embedded their ID in the token as "sub"
        // ASP.NET Core automatically parses this into User.Claims
        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                     ?? User.FindFirst("sub");
            return int.Parse(claim!.Value);
        }

        // GET api/songs
        // Returns all songs for the logged in user
        [HttpGet]
        public async Task<IActionResult> GetSongs()
        {
            var userId = GetUserId();
            var songs = await _songService.GetUserSongsAsync(userId);
            return Ok(songs);
        }

        // GET api/songs/{id}
        // Returns a single song by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSong(int id)
        {
            var userId = GetUserId();
            var song = await _songService.GetSongByIdAsync(id, userId);

            if (song == null)
                return NotFound(new { message = "Song not found." });

            return Ok(song);
        }

        // POST api/songs
        // Adds a new song to the user's library
        [HttpPost]
        public async Task<IActionResult> AddSong([FromBody] AddSongDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest(new { message = "Title is required." });

            var userId = GetUserId();
            var song = await _songService.AddSongAsync(userId, dto);

            // 201 Created with the location of the new resource
            return CreatedAtAction(nameof(GetSong), new { id = song.Id }, song);
        }

        // DELETE api/songs/{id}
        // Removes a song from the user's library
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSong(int id)
        {
            var userId = GetUserId();
            var deleted = await _songService.DeleteSongAsync(id, userId);

            if (!deleted)
                return NotFound(new { message = "Song not found." });

            return NoContent(); // 204 - success but nothing to return
        }

        // GET api/songs/count
        // Returns just the count of songs - used for the dashboard stats card
        [HttpGet("count")]
        public async Task<IActionResult> GetCount()
        {
            var userId = GetUserId();
            var count = await _songService.GetSongCountAsync(userId);
            return Ok(new { count });
        }
    }
}