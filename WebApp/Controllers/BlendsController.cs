using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Controllers;

[Authorize]
public class BlendsController(WebAppDbContext context) : Controller
{
    private readonly WebAppDbContext _context = context;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var blends = await _context.BlendMembers
            .Where(bm => bm.UserId == userId)
            .Select(bm => bm.Blend)
            .ToListAsync();

        return View(blends);
    }

    [HttpPost]
    public async Task<IActionResult> Create(string name, string friendUsernames)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var currentUserName = User.Identity?.Name;

        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["BlendError"] = "Blend name is required.";
            return RedirectToAction(nameof(Index));
        }

        var usernames = (friendUsernames ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(u => !string.Equals(u, currentUserName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (!usernames.Any())
        {
            TempData["BlendError"] = "You must invite at least one friend (and you can't invite yourself!).";
            return RedirectToAction(nameof(Index));
        }

        var friends = await _context.Users
            .Where(u => usernames.Contains(u.UserName))
            .ToListAsync();

        if (!friends.Any())
        {
            TempData["BlendError"] = "None of the invited users were found.";
            return RedirectToAction(nameof(Index));
        }

        var blend = new Blend { Name = name };
        blend.Members.Add(new BlendMember { UserId = currentUserId });
        
        foreach (var friend in friends)
        {
            blend.Members.Add(new BlendMember { UserId = friend.Id });
        }

        _context.Blends.Add(blend);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Generate(int blendId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var blend = await _context.Blends
            .Include(b => b.Members)
            .FirstOrDefaultAsync(b => b.Id == blendId);

        if (blend == null || !blend.Members.Any(m => m.UserId == userId))
        {
            return Forbid();
        }

        var memberIds = blend.Members.Select(m => m.UserId).ToList();

        // Get top tracks for all members
        var topTracks = await _context.Scrobbles
            .Where(s => memberIds.Contains(s.UserId))
            .Include(s => s.Track)
            .GroupBy(s => s.Track.ItunesTrackId)
            .OrderByDescending(g => g.Count())
            .Take(50)
            .Select(g => new
            {
                ItunesTrackId = g.Key,
                TrackTitle = g.First().Track.TrackName,
                ArtistName = g.First().Track.Artist.ArtistName,
                ArtworkUrl = g.First().Track.ArtworkUrl
            })
            .ToListAsync();

        return Json(topTracks);
    }
}
