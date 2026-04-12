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
        if ((model.ItunesTrackId <= 0) && string.IsNullOrWhiteSpace(model.FileName))
        {
            return BadRequest(new { message = "Either itunesTrackId or fileName is required" });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = string.IsNullOrWhiteSpace(userId)
            ? null
            : _context.Users.FirstOrDefault(u => u.Id == userId);

        if (user == null)
        {
            return Unauthorized(new { message = "Login required for scrobbling." });
        }

        Track? track = null;

        if (!string.IsNullOrWhiteSpace(model.FileName))
        {
            var trimmedFileName = model.FileName.Trim();
            track = _context.Tracks.FirstOrDefault(t => t.FileName == trimmedFileName);
        }

        if (track == null && model.ItunesTrackId > 0)
        {
            track = _context.Tracks.FirstOrDefault(t => t.ItunesTrackId == model.ItunesTrackId);
        }

        if (track == null)
        {
            var genreName = string.IsNullOrWhiteSpace(model.Genre) ? "Unknown" : model.Genre.Trim();
            var genre = _context.Genres.FirstOrDefault(g => g.GenreName == genreName);
            if (genre == null)
            {
                genre = new Genre { GenreName = genreName, ItunesGenreId = 0 };
                _context.Genres.Add(genre);
            }

            var artistName = string.IsNullOrWhiteSpace(model.Artist) ? "Unknown Artist" : model.Artist.Trim();
            var artist = _context.Artists.FirstOrDefault(a => a.ArtistName == artistName);
            if (artist == null)
            {
                artist = new Artist
                {
                    ItunesArtistId = 0,
                    ArtistName = artistName,
                    PrimaryGenre = genre
                };
                _context.Artists.Add(artist);
            }

            track = new Track
            {
                ItunesTrackId = model.ItunesTrackId > 0 ? model.ItunesTrackId : 0,
                FileName = model.FileName?.Trim() ?? string.Empty,
                TrackName = string.IsNullOrWhiteSpace(model.TrackTitle) ? (model.FileName?.Trim() ?? "Unknown Track") : model.TrackTitle,
                Year = model.Year,
                PreviewUrl = string.Empty,
                ArtworkUrl = string.Empty,
                Artist = artist,
                Genre = genre
            };
            _context.Tracks.Add(track);
        }
        else
        {
            if (track.ItunesTrackId <= 0 && model.ItunesTrackId > 0)
            {
                track.ItunesTrackId = model.ItunesTrackId;
            }

            if (string.IsNullOrWhiteSpace(track.FileName) && !string.IsNullOrWhiteSpace(model.FileName))
            {
                track.FileName = model.FileName.Trim();
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