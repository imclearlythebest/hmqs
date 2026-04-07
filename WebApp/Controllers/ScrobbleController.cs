using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Data;
using WebApp.Models;
using WebApp.Models.Dtos;

namespace WebApp.Controllers;

public class ScrobbleController(WebAppDbContext context) : Controller
{
    private readonly WebAppDbContext _context = context;

    [Authorize]
    [HttpPost]
    public IActionResult Submit([FromForm] ScrobbleDto model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = _context.Users.FirstOrDefault(u => u.Id == userId);
        if (user == null)
            return Unauthorized(new { message = "User not found or not authenticated" });

        var track = _context.Tracks.FirstOrDefault(t => t.ItunesTrackId == model.ItunesTrackId);
        if (track == null)
            return NotFound(new { message = $"Track with ID {model.ItunesTrackId} not found" });

        var scrobble = new Scrobble
        {
            User = user,
            Track = track,
            ScrobbledAt = DateTime.Now,
            Progress = model.Progress
        };

        _context.Scrobbles.Add(scrobble);
        _context.SaveChanges();

        return Ok(new { message = "Scrobble submitted", userId, model });
    }


}