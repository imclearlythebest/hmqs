using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;

namespace WebApp.Controllers;

public class ShareController(WebAppDbContext context, IHttpClientFactory httpClientFactory) : Controller
{
    private readonly WebAppDbContext _context = context;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    // Share by DB track ID: /Share/Track/5
    [HttpGet]
    public async Task<IActionResult> Track(int id, CancellationToken cancellationToken)
    {
        var track = await _context.Tracks
            .Include(t => t.Artist)
            .Include(t => t.Genre)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (track == null) return NotFound();

        return await RenderShareView(track, cancellationToken);
    }

    // Share by iTunes Track ID: /Share/ByItunesId/1234567
    [HttpGet]
    public async Task<IActionResult> ByItunesId(int itunesId, CancellationToken cancellationToken)
    {
        var track = await _context.Tracks
            .Include(t => t.Artist)
            .Include(t => t.Genre)
            .FirstOrDefaultAsync(t => t.ItunesTrackId == itunesId, cancellationToken);

        if (track == null)
            return View("NotFound");

        return await RenderShareView(track, cancellationToken);
    }

    private async Task<IActionResult> RenderShareView(WebApp.Models.Track track, CancellationToken cancellationToken)
    {
        ViewData["Title"] = $"Share — {track.TrackName}";
        ViewData["HidePlayer"] = true;
        ViewData["HideSidebar"] = true;

        var vm = new ShareViewModel
        {
            TrackId    = track.Id,
            TrackName  = track.TrackName,
            ArtistName = track.Artist?.ArtistName ?? "Unknown Artist",
            ArtworkUrl = track.ArtworkUrl,
            Year       = track.Year,
            GenreName  = track.GenreName ?? string.Empty,
        };

        if (track.ItunesTrackId > 0)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = $"https://api.song.link/v1-alpha.1/links?platform=itunes&type=song&id={track.ItunesTrackId}&userCountry=US";
                using var response = await client.GetAsync(url, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    var payload = await JsonSerializer.DeserializeAsync<SonglinkResponse>(
                        stream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                        cancellationToken);

                    if (payload?.LinksByPlatform != null)
                    {
                        vm.SpotifyUrl      = payload.LinksByPlatform.GetValueOrDefault("spotify")?.Url;
                        vm.AppleMusicUrl   = payload.LinksByPlatform.GetValueOrDefault("appleMusic")?.Url;
                        vm.YoutubeUrl      = payload.LinksByPlatform.GetValueOrDefault("youtube")?.Url;
                        vm.YoutubeMusicUrl = payload.LinksByPlatform.GetValueOrDefault("youtubeMusic")?.Url;
                    }
                }
            }
            catch
            {
                // Songlink unreachable — still show page
            }
        }

        return View("Track", vm);
    }

    private sealed class SonglinkResponse
    {
        public Dictionary<string, SonglinkEntry>? LinksByPlatform { get; set; }
    }

    private sealed class SonglinkEntry
    {
        public string? Url { get; set; }
    }
}

public class ShareViewModel
{
    public int    TrackId        { get; set; }
    public string TrackName      { get; set; } = string.Empty;
    public string ArtistName     { get; set; } = string.Empty;
    public string ArtworkUrl     { get; set; } = string.Empty;
    public int    Year           { get; set; }
    public string GenreName      { get; set; } = string.Empty;
    public string? SpotifyUrl      { get; set; }
    public string? AppleMusicUrl   { get; set; }
    public string? YoutubeUrl      { get; set; }
    public string? YoutubeMusicUrl { get; set; }
}
