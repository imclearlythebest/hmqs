using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace WebApp.Controllers;

[Authorize]
public class DownloadController : Controller
{
    [HttpGet]
    public IActionResult Trigger([FromQuery] string trackTitle, [FromQuery] string? artistName = null)
    {
        return BadRequest("Downloads are currently disabled for legal reasons.");
    }

    private static string BuildSearchQuery(string? trackTitle, string? artistName)
    {
        var title = (trackTitle ?? string.Empty).Trim();
        var artist = (artistName ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(title))
        {
            return string.Empty;
        }

        return string.IsNullOrWhiteSpace(artist)
            ? title
            : $"{artist} - {title}";
    }

    private static string CreateFileName(string? trackTitle, string? artistName)
    {
        var title = (trackTitle ?? string.Empty).Trim();
        var artist = (artistName ?? string.Empty).Trim();
        var baseName = string.IsNullOrWhiteSpace(artist)
            ? title
            : string.IsNullOrWhiteSpace(title)
                ? artist
                : $"{artist} - {title}";

        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "download";
        }

        var sanitized = new string(baseName.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '-' : ch).ToArray());
        return $"{sanitized}.mp3";
    }

    private static string ResolveExecutableOnPath(string executableName)
    {
        var resolved = FindExecutableOnPath(executableName);
        if (!string.IsNullOrWhiteSpace(resolved))
        {
            return resolved;
        }

        throw new InvalidOperationException($"{executableName} was not found on PATH. Install it and restart the app so the PATH change is visible to the process.");
    }

    private static string? FindExecutableOnPath(string executableName)
    {
        var pathVariable = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathVariable))
        {
            return null;
        }

        var extensions = new[] { string.Empty, ".exe", ".cmd", ".bat" };
        foreach (var directory in pathVariable.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            foreach (var extension in extensions)
            {
                var candidate = Path.Combine(directory, executableName.EndsWith(extension, StringComparison.OrdinalIgnoreCase)
                    ? executableName
                    : executableName + extension);

                if (System.IO.File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }
}
