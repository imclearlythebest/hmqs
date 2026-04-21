using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using WebApp.Models.Dtos;
using WebApp.Services;

namespace WebApp.Controllers;

[Authorize]
public class EthicalController(WebAppDbContext context, ItunesService itunesService, OdesliService odesliService) : Controller
{
    private readonly WebAppDbContext _context = context;
    private readonly ItunesService _itunesService = itunesService;
    private readonly OdesliService _odesliService = odesliService;

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] decimal? budget, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        var monthlyBudget = budget.HasValue && budget.Value > 0 ? budget.Value : 500m;
        var model = await BuildModelAsync(userId, monthlyBudget, cancellationToken);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LogPurchase(int artistId, decimal budget = 500m, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        if (artistId <= 0)
        {
            return RedirectToAction(nameof(Index), new { budget });
        }

        var stat = await _context.Set<UserArtistStat>()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.ArtistId == artistId, cancellationToken);

        if (stat != null && stat.IgnoredCount > 0)
        {
            stat.IgnoredCount = 0;
            await _context.SaveChangesAsync(cancellationToken);
        }

        TempData["EthicalMessage"] = "Purchase logged! Your support for this artist has been updated.";
        return RedirectToAction(nameof(Index), new { budget });
    }

    [HttpGet]
    public async Task<IActionResult> Purchase(int artistId, int albumId, decimal budget = 500m, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        if (artistId <= 0 || albumId <= 0)
        {
            return RedirectToAction(nameof(Index), new { budget });
        }

        var model = await BuildModelAsync(userId, budget, cancellationToken);
        var artist = model.Artists.FirstOrDefault(a => a.ArtistId == artistId && a.SuggestedAlbumId == albumId);
        if (artist == null)
        {
            return RedirectToAction(nameof(Index), new { budget });
        }

        var amazonStoreUrl = await _odesliService.GetAmazonStoreLinkForAlbumAsync(albumId);
        if (string.IsNullOrWhiteSpace(amazonStoreUrl))
        {
            TempData["EthicalMessage"] = "Could not resolve the Amazon store link for that album right now.";
            return RedirectToAction(nameof(Index), new { budget });
        }

        return View("PurchaseRedirect", new EthicalPurchaseRedirectViewDto
        {
            ArtistName = artist.ArtistName,
            AlbumName = artist.SuggestedAlbumName ?? "Suggested album",
            AlbumPrice = artist.SuggestedAlbumPrice,
            AmazonStoreUrl = amazonStoreUrl,
            Budget = budget,
        });
    }

    private async Task<EthicalCalculatorViewDto> BuildModelAsync(string userId, decimal monthlyBudget, CancellationToken cancellationToken)
    {
        var userArtistSecondsRows = await _context.Scrobbles
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .Join(
                _context.Tracks.AsNoTracking(),
                scrobble => scrobble.TrackId,
                track => track.Id,
                (scrobble, track) => new { track.ArtistId, scrobble.DurationSeconds })
            .Where(x => x.ArtistId.HasValue && x.DurationSeconds > 0)
            .GroupBy(x => x.ArtistId!.Value)
            .Select(g => new { ArtistId = g.Key, Seconds = g.Sum(x => x.DurationSeconds) })
            .ToListAsync(cancellationToken);

        if (userArtistSecondsRows.Count == 0)
        {
            return new EthicalCalculatorViewDto
            {
                MonthlyBudget = monthlyBudget,
                Artists = []
            };
        }

        var globalArtistSecondsRows = await _context.Scrobbles
            .AsNoTracking()
            .Join(
                _context.Tracks.AsNoTracking(),
                scrobble => scrobble.TrackId,
                track => track.Id,
                (scrobble, track) => new { track.ArtistId, scrobble.DurationSeconds })
            .Where(x => x.ArtistId.HasValue && x.DurationSeconds > 0)
            .GroupBy(x => x.ArtistId!.Value)
            .Select(g => new { ArtistId = g.Key, Seconds = g.Sum(x => x.DurationSeconds) })
            .ToListAsync(cancellationToken);

        var artistIds = userArtistSecondsRows.Select(x => x.ArtistId).ToList();
        var artistRows = await _context.Artists
            .AsNoTracking()
            .Where(a => artistIds.Contains(a.Id))
            .Select(a => new { a.Id, a.ArtistName, a.ItunesArtistId })
            .ToListAsync(cancellationToken);

        var ignoredRows = await _context.Set<UserArtistStat>()
            .AsNoTracking()
            .Where(s => s.UserId == userId && artistIds.Contains(s.ArtistId))
            .Select(s => new { s.ArtistId, s.IgnoredCount })
            .ToListAsync(cancellationToken);

        var listeningWeightSum = userArtistSecondsRows.Sum(x => Math.Sqrt(Math.Max(0, x.Seconds)));
        var maxGlobalSeconds = Math.Max(1, globalArtistSecondsRows.Select(x => x.Seconds).DefaultIfEmpty(0).Max());
        var maxGlobalLog = Math.Log(1 + maxGlobalSeconds);

        var globalByArtist = globalArtistSecondsRows.ToDictionary(x => x.ArtistId, x => x.Seconds);
        var nameByArtist = artistRows.ToDictionary(x => x.Id, x => x.ArtistName);
        var itunesIdByArtist = artistRows.ToDictionary(x => x.Id, x => x.ItunesArtistId);
        var ignoredByArtist = ignoredRows.ToDictionary(x => x.ArtistId, x => x.IgnoredCount);

        const decimal alpha = 0.75m;
        const decimal beta = 0.25m;

        var scored = new List<(EthicalArtistScoreDto dto, decimal rawScore, decimal rawListeningShare)>();
        foreach (var row in userArtistSecondsRows)
        {
            var globalSeconds = globalByArtist.GetValueOrDefault(row.ArtistId, 0);
            var ignoredCount = ignoredByArtist.GetValueOrDefault(row.ArtistId, 0);

            var listeningWeight = Math.Sqrt(Math.Max(0, row.Seconds));
            var listeningShare = listeningWeightSum > 0
                ? (decimal)(listeningWeight / listeningWeightSum)
                : 0m;

            var ignoredShare = ignoredCount > 0
                ? (decimal)ignoredCount / (ignoredCount + 3m)
                : 0m;

            var popularity = maxGlobalLog > 0
                ? (decimal)(Math.Log(1 + globalSeconds) / maxGlobalLog)
                : 0m;

            var inversePopularity = 0.2m + (0.8m * (1m - popularity));
            var listenComponent = alpha * listeningShare * inversePopularity;
            var ignoreComponent = beta * ignoredShare * (1m + inversePopularity);
            var rawScore = listenComponent + ignoreComponent;

            var dto = new EthicalArtistScoreDto
            {
                ArtistId = row.ArtistId,
                ItunesArtistId = itunesIdByArtist.GetValueOrDefault(row.ArtistId),
                ArtistName = nameByArtist.GetValueOrDefault(row.ArtistId, "Unknown Artist"),
                UserListenedSeconds = row.Seconds,
                GlobalListenedSeconds = globalSeconds,
                IgnoredCount = ignoredCount,
                InversePopularity = Math.Round(inversePopularity, 4),
                ListeningShare = Math.Round(listeningShare, 4),
                IgnoredShare = Math.Round(ignoredShare, 4),
                EthicalScore = Math.Round(rawScore, 6),
            };

            scored.Add((dto, rawScore, listeningShare));
        }

        var totalScore = scored.Sum(x => x.rawScore);
        var withBudget = scored
            .Select(x =>
            {
                var fallbackShare = x.rawListeningShare;
                var effectiveShare = totalScore > 0
                    ? x.rawScore / totalScore
                    : fallbackShare;

                return new EthicalArtistScoreDto
                {
                    ArtistId = x.dto.ArtistId,
                    ItunesArtistId = x.dto.ItunesArtistId,
                    ArtistName = x.dto.ArtistName,
                    UserListenedSeconds = x.dto.UserListenedSeconds,
                    GlobalListenedSeconds = x.dto.GlobalListenedSeconds,
                    IgnoredCount = x.dto.IgnoredCount,
                    InversePopularity = x.dto.InversePopularity,
                    ListeningShare = x.dto.ListeningShare,
                    IgnoredShare = x.dto.IgnoredShare,
                    EthicalScore = x.dto.EthicalScore,
                    SuggestedBudget = Math.Round(monthlyBudget * effectiveShare, 2)
                };
            })
            .OrderByDescending(x => x.EthicalScore)
            .ToList();

        // Parallelize album lookups to avoid sequential API calls
        var albumLookupTasks = withBudget
            .Select(async artist => new
            {
                Artist = artist,
                Result = await _itunesService.GetBestAlbumAsync(artist.ItunesArtistId, artist.SuggestedBudget)
            })
            .ToList();

        var albumResults = await Task.WhenAll(albumLookupTasks);

        foreach (var item in albumResults)
        {
            item.Artist.SuggestedAlbumName = item.Result.Name;
            item.Artist.SuggestedAlbumId = item.Result.Id;
            item.Artist.SuggestedAlbumPrice = item.Result.Price;
        }

        return new EthicalCalculatorViewDto
        {
            MonthlyBudget = monthlyBudget,
            Artists = withBudget
        };
    }
}
