using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using WebApp.Models.Dtos;
using WebApp.Models.ViewModels;

namespace WebApp.Controllers;

public class ShareController(WebAppDbContext context) : Controller
{
    private readonly WebAppDbContext _context = context;
    private static readonly HttpClient _httpClient = new HttpClient();

    [HttpGet]
    public async Task<IActionResult> Track(int itunesId, string title, string artist, string album, string imageUrl)
    {
        ViewData["HidePlayer"] = true;
        ViewData["HideSidebar"] = true;

        var model = await FetchTrackShareDataAsync(itunesId, title, artist, album, imageUrl);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePlaylistShare([FromBody] SharedPlaylistDto payload)
    {
        if (payload == null || string.IsNullOrWhiteSpace(payload.Name) || payload.Tracks.Count == 0)
        {
            return BadRequest("Invalid playlist data");
        }

        var sharedPlaylist = new SharedPlaylist
        {
            Name = payload.Name,
            Tracks = payload.Tracks.Select(t => new SharedPlaylistTrack
            {
                Title = t.Title,
                Artist = t.Artist,
                Album = t.Album,
                ItunesTrackId = t.ItunesTrackId,
                ImageUrl = t.ImageUrl
            }).ToList()
        };

        _context.SharedPlaylists.Add(sharedPlaylist);
        await _context.SaveChangesAsync();

        return Ok(new { id = sharedPlaylist.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Playlist(string id)
    {
        ViewData["HidePlayer"] = true;
        ViewData["HideSidebar"] = true;

        var playlist = await _context.SharedPlaylists
            .Include(p => p.Tracks)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (playlist == null)
        {
            return NotFound();
        }

        var model = new PlaylistShareViewModel
        {
            Name = playlist.Name
        };

        // For simplicity, we might not fetch universal links for EVERY track on page load to avoid rate limits,
        // but since it's a small app, let's fetch them in parallel.
        var tasks = playlist.Tracks.Select(t => FetchTrackShareDataAsync(t.ItunesTrackId, t.Title, t.Artist, t.Album, t.ImageUrl));
        var tracks = await Task.WhenAll(tasks);
        model.Tracks.AddRange(tracks);

        return View(model);
    }

    private async Task<TrackShareViewModel> FetchTrackShareDataAsync(int itunesId, string title, string artist, string album, string imageUrl)
    {
        var model = new TrackShareViewModel
        {
            Title = title ?? "Unknown Track",
            Artist = artist ?? string.Empty,
            Album = album ?? string.Empty,
            ImageUrl = imageUrl ?? string.Empty
        };

        string sourceUrl = string.Empty;
        if (itunesId > 0)
        {
            sourceUrl = $"https://music.apple.com/us/song/{itunesId}";
        }
        else if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(artist))
        {
            try
            {
                var searchUrl = $"https://itunes.apple.com/search?term={Uri.EscapeDataString(artist + " " + title)}&entity=song&limit=1";
                var response = await _httpClient.GetAsync(searchUrl);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(content);
                    if (doc.RootElement.GetProperty("resultCount").GetInt32() > 0)
                    {
                        var result = doc.RootElement.GetProperty("results")[0];
                        if (result.TryGetProperty("trackViewUrl", out var trackViewUrl))
                        {
                            sourceUrl = trackViewUrl.GetString() ?? string.Empty;
                        }
                    }
                }
            }
            catch
            {
                // Ignore search errors
            }
        }

        if (!string.IsNullOrWhiteSpace(sourceUrl))
        {
            try
            {
                var odesliUrl = $"https://api.song.link/v1-alpha.1/links?url={Uri.EscapeDataString(sourceUrl)}";
                var response = await _httpClient.GetAsync(odesliUrl);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(content);
                    
                    if (doc.RootElement.TryGetProperty("linksByPlatform", out var linksNode))
                    {
                        foreach (var prop in linksNode.EnumerateObject())
                        {
                            var platform = prop.Name;
                            if (prop.Value.TryGetProperty("url", out var urlNode))
                            {
                                model.PlatformUrls[platform] = urlNode.GetString() ?? string.Empty;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignore Odesli errors
            }
        }

        return model;
    }
}
