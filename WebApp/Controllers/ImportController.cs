using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Controllers;

[Authorize]
public class ImportController(WebAppDbContext context) : Controller
{
    private readonly WebAppDbContext _context = context;

    [HttpGet]
    public IActionResult Index()
    {
        ViewData["Title"] = "Import from Spotify";
        ViewData["HidePlayer"] = true;
        ViewData["HideSidebar"] = true;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> UploadSpotify(IFormFile? playlistFile, IFormFile? historyFile, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Import from Spotify";
        ViewData["HidePlayer"] = true;
        ViewData["HideSidebar"] = true;

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var results = new ImportResultViewModel();

        if (playlistFile != null && playlistFile.Length > 0)
        {
            await using var stream = playlistFile.OpenReadStream();
            var spotifyPlaylists = await JsonSerializer.DeserializeAsync<List<SpotifyPlaylist>>(
                stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken) ?? [];

            foreach (var sp in spotifyPlaylists)
            {
                var playlist = new Playlist
                {
                    Name = sp.Name ?? "Imported Playlist",
                    UserId = userId,
                    CreatedAtUtc = DateTime.UtcNow
                };
                _context.Playlists.Add(playlist);
                await _context.SaveChangesAsync(cancellationToken);

                int order = 0;
                int matched = 0;
                int skipped = 0;

                foreach (var item in sp.Items ?? [])
                {
                    var trackName = item.Track?.TrackName?.Trim();
                    var artistName = item.Track?.ArtistName?.Trim();

                    if (string.IsNullOrWhiteSpace(trackName))
                    {
                        skipped++;
                        continue;
                    }

                    var track = await _context.Tracks
                        .Include(t => t.Artist)
                        .FirstOrDefaultAsync(t =>
                            t.TrackName.ToLower() == trackName.ToLower() &&
                            (artistName == null || t.Artist!.ArtistName.ToLower() == artistName.ToLower()),
                            cancellationToken);

                    if (track != null)
                    {
                        _context.PlaylistTracks.Add(new PlaylistTrack
                        {
                            PlaylistId = playlist.Id,
                            TrackId = track.Id,
                            Order = order++
                        });
                        matched++;
                    }
                    else
                    {
                        skipped++;
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);
                results.ImportedPlaylists.Add(new PlaylistImportResult
                {
                    Name = playlist.Name,
                    Matched = matched,
                    Skipped = skipped
                });
            }
        }

        if (historyFile != null && historyFile.Length > 0)
        {
            await using var stream = historyFile.OpenReadStream();
            var history = await JsonSerializer.DeserializeAsync<List<SpotifyHistoryEntry>>(
                stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken) ?? [];

            int imported = 0;
            int skipped = 0;

            foreach (var entry in history)
            {
                var trackName = entry.MasterMetadataTrackName?.Trim();
                if (string.IsNullOrWhiteSpace(trackName)) { skipped++; continue; }

                var track = await _context.Tracks
                    .FirstOrDefaultAsync(t => t.TrackName.ToLower() == trackName.ToLower(), cancellationToken);

                if (track == null) { skipped++; continue; }

                var alreadyScrobbled = await _context.Scrobbles.AnyAsync(
                    s => s.UserId == userId && s.TrackId == track.Id && s.ScrobbledAt == entry.Ts,
                    cancellationToken);

                if (!alreadyScrobbled)
                {
                    var msPlayed = entry.MsPlayed ?? 0;
                    _context.Scrobbles.Add(new Scrobble
                    {
                        UserId = userId,
                        TrackId = track.Id,
                        ScrobbledAt = entry.Ts ?? DateTime.UtcNow,
                        DurationSeconds = (int)(msPlayed / 1000),
                        Progress = 1m
                    });
                    imported++;
                }
                else skipped++;
            }

            await _context.SaveChangesAsync(cancellationToken);
            results.HistoryImported = imported;
            results.HistorySkipped = skipped;
        }

        ViewBag.Results = results;
        return View("Index");
    }

    // ── Spotify JSON shapes ──────────────────────────────────────────────────

    private sealed class SpotifyPlaylist
    {
        public string? Name { get; set; }
        public List<SpotifyPlaylistItem>? Items { get; set; }
    }

    private sealed class SpotifyPlaylistItem
    {
        public SpotifyTrackRef? Track { get; set; }
    }

    private sealed class SpotifyTrackRef
    {
        public string? TrackName { get; set; }
        public string? ArtistName { get; set; }
    }

    private sealed class SpotifyHistoryEntry
    {
        public DateTime? Ts { get; set; }
        public string? MasterMetadataTrackName { get; set; }
        public long? MsPlayed { get; set; }
    }
}

// ── View models ──────────────────────────────────────────────────────────────

public class ImportResultViewModel
{
    public List<PlaylistImportResult> ImportedPlaylists { get; set; } = new();
    public int HistoryImported { get; set; }
    public int HistorySkipped { get; set; }
}

public class PlaylistImportResult
{
    public string Name { get; set; } = string.Empty;
    public int Matched { get; set; }
    public int Skipped { get; set; }
}
