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
    public async Task<IActionResult> Trigger([FromQuery] string trackTitle, [FromQuery] string? artistName = null)
    {
        try
        {
            var searchQuery = BuildSearchQuery(trackTitle, artistName);
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                return BadRequest("Track title is required for download.");
            }

            var ytdl = new YoutubeDL
            {
                YoutubeDLPath = ResolveExecutableOnPath("yt-dlp"),
                FFmpegPath = ResolveExecutableOnPath("ffmpeg"),
                OutputFolder = Path.GetTempPath()
            };

            var res = await ytdl.RunAudioDownload(
                $"ytsearch1:{searchQuery}",
                AudioConversionFormat.Mp3
            );

            if (!res.Success)
            {
                var errorText = res.ErrorOutput is { Length: > 0 }
                    ? string.Join(", ", res.ErrorOutput)
                    : "Unknown downloader error.";
                return StatusCode(500, $"Download failed: {errorText}");
            }

            var tempPath = res.Data;
            if (string.IsNullOrWhiteSpace(tempPath) || !System.IO.File.Exists(tempPath))
            {
                return StatusCode(500, "Download finished but no output audio file was produced.");
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(tempPath);
            System.IO.File.Delete(tempPath);

            var fileName = CreateFileName(trackTitle, artistName);
            return File(fileBytes, "audio/mpeg", fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
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
