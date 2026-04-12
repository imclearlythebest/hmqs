using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using WebApp.Models.Dtos;

namespace WebApp.Controllers;

public class ScrobbleController(WebAppDbContext context) : Controller
{
    private readonly WebAppDbContext _context = context;

    [Authorize]
    [HttpPost]
    public IActionResult Submit([FromBody] ScrobbleDto model)
    {
        if (model.ItunesTrackId <= 0)
        {
            return BadRequest(new { message = "itunesTrackId is required" });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = string.IsNullOrWhiteSpace(userId)
            ? null
            : _context.Users.FirstOrDefault(u => u.Id == userId);

        if (user == null)
        {
            return Unauthorized(new { message = "Login required for scrobbling." });
        }

        Track? track = _context.Tracks
            .Include(t => t.Artist)
            .ThenInclude(a => a.PrimaryGenre)
            .Include(t => t.Genre)
            .FirstOrDefault(t => t.ItunesTrackId == model.ItunesTrackId);

        if (track == null)
        {
            var genreName = "Unknown";
            var genre = _context.Genres.FirstOrDefault(g => g.GenreName == genreName);
            if (genre == null)
            {
                genre = new Genre { GenreName = genreName, ItunesGenreId = 0 };
                _context.Genres.Add(genre);
            }

            Artist? artist = null;
            if (model.ItunesArtistId.HasValue && model.ItunesArtistId.Value > 0)
            {
                artist = _context.Artists
                    .Include(a => a.PrimaryGenre)
                    .FirstOrDefault(a => a.ItunesArtistId == model.ItunesArtistId.Value);
            }

            if (artist == null)
            {
                artist = new Artist
                {
                    ItunesArtistId = model.ItunesArtistId.HasValue && model.ItunesArtistId.Value > 0 ? model.ItunesArtistId.Value : 0,
                    ArtistName = "Unknown Artist",
                    PrimaryGenre = genre
                };
                _context.Artists.Add(artist);
            }

            track = new Track
            {
                ItunesTrackId = model.ItunesTrackId,
                FileName = string.Empty,
                TrackName = "Unknown Track",
                Year = 0,
                PreviewUrl = string.Empty,
                ArtworkUrl = string.Empty,
                Artist = artist,
                Genre = artist.PrimaryGenre ?? genre
            };
            _context.Tracks.Add(track);
        }
        else
        {
            if (track.ItunesTrackId <= 0)
            {
                track.ItunesTrackId = model.ItunesTrackId;
            }

            if (track.Artist != null && track.Artist.ItunesArtistId <= 0 && model.ItunesArtistId.HasValue && model.ItunesArtistId.Value > 0)
            {
                track.Artist.ItunesArtistId = model.ItunesArtistId.Value;
            }
        }

        var scrobble = new Scrobble
        {
            User = user,
            Track = track,
            ScrobbledAt = DateTime.Now,
            Progress = model.Progress,
            DurationSeconds = model.DurationSeconds
        };

        _context.Scrobbles.Add(scrobble);
        _context.SaveChanges();

        return Ok(new { message = "Scrobble submitted", userId, trackId = track.Id, model.Progress, model.DurationSeconds });
    }

    [Authorize]
    [HttpGet]
    public IActionResult History()
    {
        ViewData["HidePlayer"] = true;
        ViewData["HideSidebar"] = true;

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var query = _context.Scrobbles
            .Include(s => s.Track)
            .ThenInclude(t => t.Artist)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(s => s.User != null && s.User.Id == userId);
        }

        var history = query
            .OrderByDescending(s => s.ScrobbledAt)
            .Take(100)
            .ToList();

        return View(history);
    }
}