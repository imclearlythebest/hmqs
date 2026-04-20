using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using WebApp.Models.Dtos;

namespace WebApp.Controllers;

[Authorize]
public class ImportController(WebAppDbContext context) : Controller
{
    private readonly WebAppDbContext _context = context;

    [HttpGet]
    public IActionResult Index()
    {
        ViewData["Title"] = "Import Data";
        ViewData["HidePlayer"] = true;
        ViewData["HideSidebar"] = true;
        return View();
    }

    [HttpPost]
    [RequestSizeLimit(100_000_000)] // Allow large history files up to 100MB
    public async Task<IActionResult> History(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return Unauthorized();

        List<SpotifyHistoryItem>? historyItems;
        try
        {
            using var stream = file.OpenReadStream();
            historyItems = await JsonSerializer.DeserializeAsync<List<SpotifyHistoryItem>>(stream);
        }
        catch
        {
            return BadRequest("Invalid JSON format.");
        }

        if (historyItems == null || historyItems.Count == 0)
        {
            return BadRequest("No history items found.");
        }

        var newScrobbles = new List<Scrobble>();

        var artistCache = await _context.Artists.ToDictionaryAsync(a => a.ArtistName);
        var trackCache = await _context.Tracks.ToDictionaryAsync(t => t.TrackName + "||" + t.ArtistId);
        
        var unknownGenre = await _context.Genres.FirstOrDefaultAsync(g => g.GenreName == "Unknown");
        if (unknownGenre == null)
        {
            unknownGenre = new Genre { GenreName = "Unknown" };
            _context.Genres.Add(unknownGenre);
            await _context.SaveChangesAsync();
        }

        foreach (var item in historyItems)
        {
            var artistName = item.artistName ?? item.master_metadata_album_artist_name;
            var trackName = item.trackName ?? item.master_metadata_track_name;
            var timestampStr = item.endTime ?? item.ts;
            var msPlayed = item.msPlayed ?? item.ms_played ?? 0;

            if (string.IsNullOrWhiteSpace(artistName) || string.IsNullOrWhiteSpace(trackName) || string.IsNullOrWhiteSpace(timestampStr))
                continue;
                
            if (!DateTime.TryParse(timestampStr, out var scrobbledAt))
                continue;

            if (!artistCache.TryGetValue(artistName, out var artist))
            {
                artist = new Artist
                {
                    ArtistName = artistName,
                    PrimaryGenre = unknownGenre,
                    PrimaryGenreName = "Unknown"
                };
                _context.Artists.Add(artist);
                artistCache[artistName] = artist;
            }

            var trackKey = trackName + "||" + artist.Id;
            if (artist.Id == 0 || !trackCache.TryGetValue(trackKey, out var track))
            {
                track = new Track
                {
                    TrackName = trackName,
                    Artist = artist,
                    Genre = unknownGenre,
                    GenreName = "Unknown"
                };
                _context.Tracks.Add(track);
                if (artist.Id > 0) trackCache[trackKey] = track;
            }

            var durationSeconds = msPlayed / 1000;
            var progress = durationSeconds > 30 ? 100m : 0m;

            newScrobbles.Add(new Scrobble
            {
                UserId = user.Id,
                User = user,
                Track = track,
                ScrobbledAt = scrobbledAt,
                DurationSeconds = durationSeconds,
                Progress = progress
            });
        }

        _context.Scrobbles.AddRange(newScrobbles);
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Imported {newScrobbles.Count} scrobbles." });
    }
}
