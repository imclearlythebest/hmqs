using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApp.Data;
using WebApp.Models;
using Microsoft.AspNetCore.Identity;
using WebApp.Services;

namespace WebApp.Controllers;

[Authorize]
public class ActivityController(WebAppDbContext context, DiscordService discordService, UserManager<WebAppUser> userManager) : Controller
{
    private readonly WebAppDbContext _context = context;
    private readonly DiscordService _discordService = discordService;
    private readonly UserManager<WebAppUser> _userManager = userManager;

    [HttpPost]
    public async Task<IActionResult> Notify([FromBody] NotifyNowPlayingDto? model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var itunesTrackId = model?.ItunesTrackId ?? 0;
        var title = model?.TrackTitle?.Trim() ?? string.Empty;
        var artist = model?.ArtistName?.Trim() ?? string.Empty;
        var artworkUrl = model?.ArtworkUrl?.Trim() ?? string.Empty;

        if (itunesTrackId > 0)
        {
            var track = await _context.Tracks
                .AsNoTracking()
                .Include(t => t.Artist)
                .FirstOrDefaultAsync(t => t.ItunesTrackId == itunesTrackId);

            if (track != null)
            {
                title = string.IsNullOrWhiteSpace(title) ? (track.TrackName ?? string.Empty) : title;
                artist = string.IsNullOrWhiteSpace(artist) ? (track.Artist?.ArtistName ?? string.Empty) : artist;
                artworkUrl = string.IsNullOrWhiteSpace(artworkUrl) ? (track.ArtworkUrl ?? string.Empty) : artworkUrl;
            }
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            return BadRequest("Track title is required.");
        }

        if (string.IsNullOrWhiteSpace(artist))
        {
            artist = "Unknown Artist";
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user != null && !string.IsNullOrWhiteSpace(user.DiscordWebhookUrl))
        {
            await _discordService.PostNowPlayingAsync(
                user.DiscordWebhookUrl, 
                title,
                artist,
                artworkUrl,
                user.UserName ?? "User");
        }

        return Ok();
    }

    public sealed class NotifyNowPlayingDto
    {
        public int ItunesTrackId { get; set; }
        public string? TrackTitle { get; set; }
        public string? ArtistName { get; set; }
        public string? ArtworkUrl { get; set; }
    }

}
