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
            .Include(t => t.Artist!)
            .ThenInclude(a => a.PrimaryGenre)
            .Include(t => t.Genre)
            .Include(t => t.Collection)
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
                    PrimaryGenreName = genre.GenreName,
                    PrimaryGenre = genre
                };
                _context.Artists.Add(artist);
            }

            var resolvedArtist = artist;

            Collection? collection = null;
            if (model.ItunesCollectionId.HasValue && model.ItunesCollectionId.Value > 0)
            {
                collection = _context.Albums
                    .Include(c => c.Artist)
                    .Include(c => c.Genre)
                    .FirstOrDefault(c => c.ItunesCollectionId == model.ItunesCollectionId.Value);

                if (collection == null)
                {
                    collection = new Collection
                    {
                        ItunesCollectionId = model.ItunesCollectionId.Value,
                        Artist = resolvedArtist,
                        ArtistId = resolvedArtist.Id > 0 ? resolvedArtist.Id : null,
                        Genre = resolvedArtist.PrimaryGenre ?? genre,
                        GenreName = (resolvedArtist.PrimaryGenre ?? genre).GenreName
                    };
                    _context.Albums.Add(collection);
                }
            }

            track = new Track
            {
                ItunesTrackId = model.ItunesTrackId,
                FileName = string.Empty,
                TrackName = "Unknown Track",
                Year = 0,
                PreviewUrl = string.Empty,
                ArtworkUrl = string.Empty,
                ArtistId = resolvedArtist.Id > 0 ? resolvedArtist.Id : null,
                Artist = resolvedArtist,
                GenreName = (resolvedArtist.PrimaryGenre ?? genre).GenreName,
                Genre = resolvedArtist.PrimaryGenre ?? genre,
                CollectionId = collection?.Id > 0 ? collection.Id : null,
                Collection = collection
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

            if (track.Collection == null && model.ItunesCollectionId.HasValue && model.ItunesCollectionId.Value > 0)
            {
                var collection = _context.Albums
                    .Include(c => c.Artist)
                    .Include(c => c.Genre)
                    .FirstOrDefault(c => c.ItunesCollectionId == model.ItunesCollectionId.Value);

                if (collection == null)
                {
                    collection = new Collection
                    {
                        ItunesCollectionId = model.ItunesCollectionId.Value,
                        Artist = track.Artist,
                        ArtistId = track.ArtistId,
                        Genre = track.Genre,
                        GenreName = track.GenreName
                    };
                    _context.Albums.Add(collection);
                }

                track.Collection = collection;
                if (collection.Id > 0)
                {
                    track.CollectionId = collection.Id;
                }
            }
        }

        var scrobble = new Scrobble
        {
            UserId = user.Id,
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
            query = query.Where(s => s.UserId == userId);
        }

        var history = query
            .OrderByDescending(s => s.ScrobbledAt)
            .Take(100)
            .ToList();

        return View(history);
    }
}