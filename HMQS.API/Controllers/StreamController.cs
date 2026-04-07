using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using HMQS.API.Data;

namespace HMQS.API.Controllers
{
    [ApiController]
    [Route("api/stream")]
    public class StreamController : ControllerBase
    {
        private readonly AppDbContext _db;

        public StreamController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("{songId}")]
        [AllowAnonymous] // Temporarily allow anonymous for testing
        public async Task<IActionResult> Stream(int songId)
        {
            var song = await _db.Songs
                .FirstOrDefaultAsync(s => s.Id == songId);

            if (song == null)
                return NotFound(new { message = "Song not found." });

            if (string.IsNullOrEmpty(song.FilePath))
                return BadRequest(new { message = "No file path set." });

            if (!System.IO.File.Exists(song.FilePath))
                return NotFound(new { message = $"File not found: {song.FilePath}" });

            var extension = Path.GetExtension(song.FilePath).ToLowerInvariant();
            var mimeType = extension switch
            {
                ".mp3" => "audio/mpeg",
                ".wav" => "audio/wav",
                ".m4a" => "audio/mp4",
                ".aac" => "audio/aac",
                ".ogg" => "audio/ogg",
                ".flac" => "audio/flac",
                _ => "application/octet-stream"
            };

            return PhysicalFile(
                song.FilePath,
                mimeType,
                enableRangeProcessing: true
            );
        }
    }
}