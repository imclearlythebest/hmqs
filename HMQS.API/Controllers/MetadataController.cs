using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using HMQS.API.Data;
using HMQS.API.DTOs;
using HMQS.API.Models;
using HMQS.API.Services;

namespace HMQS.API.Controllers
{
    [ApiController]
    [Route("api/metadata")]
    [Authorize]
    public class MetadataController : ControllerBase
    {
        private readonly ItunesService _itunes;
        private readonly AppDbContext _db;

        public MetadataController(ItunesService itunes, AppDbContext db)
        {
            _itunes = itunes;
            _db = db;
        }

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                     ?? User.FindFirst("sub");
            return int.Parse(claim!.Value);
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { message = "Search query is required." });

            var results = await _itunes.SearchAsync(q);
            return Ok(results);
        }

        [HttpPost("apply")]
        public async Task<IActionResult> Apply([FromBody] ApplyMetadataDto dto)
        {
            var userId = GetUserId();

            var song = await _db.Songs
                .FirstOrDefaultAsync(s => s.Id == dto.SongId && s.UserId == userId);

            if (song == null)
                return NotFound(new { message = "Song not found." });

            song.Title = dto.Title;
            song.MusicBrainzId = dto.MusicBrainzId;
            song.CoverArtUrl = dto.CoverArtUrl;

            if (!string.IsNullOrEmpty(dto.ArtistName))
            {
                var artist = await _db.Artists
                    .FirstOrDefaultAsync(a => a.Name == dto.ArtistName);

                if (artist == null)
                {
                    artist = new Artist { Name = dto.ArtistName };
                    _db.Artists.Add(artist);
                    await _db.SaveChangesAsync();
                }

                if (!string.IsNullOrEmpty(dto.AlbumTitle))
                {
                    var album = await _db.Albums
                        .FirstOrDefaultAsync(a =>
                            a.Title == dto.AlbumTitle &&
                            a.ArtistId == artist.Id);

                    if (album == null)
                    {
                        album = new Album
                        {
                            Title = dto.AlbumTitle,
                            ArtistId = artist.Id,
                            ReleaseYear = dto.ReleaseYear,
                            CoverArtUrl = dto.CoverArtUrl
                        };
                        _db.Albums.Add(album);
                        await _db.SaveChangesAsync();
                    }

                    song.AlbumId = album.Id;
                }
            }

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Metadata applied.",
                songId = song.Id,
                title = song.Title,
                artist = dto.ArtistName,
                album = dto.AlbumTitle,
                year = dto.ReleaseYear,
                coverArtUrl = song.CoverArtUrl
            });
        }
    }
}