using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApp.Models;

namespace WebApp.Controllers;

[Authorize]
public class SettingsController(UserManager<WebAppUser> userManager) : Controller
{
    private readonly UserManager<WebAppUser> _userManager = userManager;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDiscord(string discordWebhookUrl)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        user.DiscordWebhookUrl = discordWebhookUrl;
        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            TempData["SettingsMessage"] = "Discord Webhook updated successfully!";
        }
        else
        {
            TempData["SettingsError"] = "Failed to update Discord Webhook.";
        }

        return RedirectToAction(nameof(Index));
    }
}
