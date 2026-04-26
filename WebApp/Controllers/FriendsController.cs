using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Controllers;

[Authorize]
public class FriendsController(WebAppDbContext context) : Controller
{
    private readonly WebAppDbContext _context = context;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var friends = await _context.Friendships
            .Where(f => (f.RequesterId == userId || f.ReceiverId == userId) && f.Status == FriendshipStatus.Accepted)
            .Select(f => f.RequesterId == userId ? f.Receiver : f.Requester)
            .ToListAsync();

        var pendingRequests = await _context.Friendships
            .Where(f => f.ReceiverId == userId && f.Status == FriendshipStatus.Pending)
            .Include(f => f.Requester)
            .ToListAsync();

        ViewBag.PendingRequests = pendingRequests;
        return View(friends);
    }

    [HttpPost]
    public async Task<IActionResult> SendRequest(string username)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var receiver = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);

        if (receiver == null || receiver.Id == currentUserId)
        {
            TempData["FriendError"] = "User not found or invalid.";
            return RedirectToAction(nameof(Index));
        }

        var existing = await _context.Friendships.FirstOrDefaultAsync(f =>
            (f.RequesterId == currentUserId && f.ReceiverId == receiver.Id) ||
            (f.RequesterId == receiver.Id && f.ReceiverId == currentUserId));

        if (existing != null)
        {
            TempData["FriendError"] = "Request already exists.";
            return RedirectToAction(nameof(Index));
        }

        _context.Friendships.Add(new Friendship
        {
            RequesterId = currentUserId,
            ReceiverId = receiver.Id,
            Status = FriendshipStatus.Pending
        });

        await _context.SaveChangesAsync();
        TempData["FriendMessage"] = "Request sent!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> AcceptRequest(int requestId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var request = await _context.Friendships.FirstOrDefaultAsync(f => f.Id == requestId && f.ReceiverId == userId);

        if (request != null)
        {
            request.Status = FriendshipStatus.Accepted;
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> RecentScrobbles()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var friendIds = await _context.Friendships
            .Where(f => (f.RequesterId == userId || f.ReceiverId == userId) && f.Status == FriendshipStatus.Accepted)
            .Select(f => f.RequesterId == userId ? f.ReceiverId : f.RequesterId)
            .ToListAsync();

        var scrobbles = await _context.Scrobbles
            .Where(s => friendIds.Contains(s.UserId))
            .Include(s => s.User)
            .Include(s => s.Track)
            .ThenInclude(t => t.Artist)
            .OrderByDescending(s => s.ScrobbledAt)
            .Take(20)
            .Select(s => new
            {
                Username = s.User.UserName,
                TrackTitle = s.Track.TrackName,
                ArtistName = s.Track.Artist.ArtistName,
                PlayedAt = s.ScrobbledAt.Kind == DateTimeKind.Utc ? s.ScrobbledAt : DateTime.SpecifyKind(s.ScrobbledAt, DateTimeKind.Utc)
            })
            .ToListAsync();

        return Json(scrobbles);
    }
}
