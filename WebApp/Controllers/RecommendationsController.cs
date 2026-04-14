using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models.Dtos;

namespace WebApp.Controllers;

public class RecommendationsController(IHttpClientFactory httpClientFactory, WebAppDbContext context) : Controller
{
    private const int PageSize = 10;
    private const int MaxPages = 5;
    private const int MaxRecommendations = PageSize * MaxPages;
    private const string ItunesFeedUrl = "https://rss.marketingtools.apple.com/api/v2/us/music/most-played/100/songs.json";
    private readonly WebAppDbContext _context = context;

    [HttpGet]
    public IActionResult Index() => View();

    [HttpGet]
    public async Task<IActionResult> Feed([FromQuery] int page = 1, CancellationToken cancellationToken = default)
    {
        if (page < 1 || page > MaxPages)
        {
            return PartialView("_RecommendationBatch", new RecommendationBatchDto
            {
                FeedAction = "Feed"
            });
        }

        try
        {
            var allRecommendations = await LoadRecommendationsAsync(cancellationToken);
            var cappedRecommendations = allRecommendations.Take(MaxRecommendations).ToList();
            var offset = (page - 1) * PageSize;
            var pageItems = cappedRecommendations.Skip(offset).Take(PageSize).ToList();
            var hasMore = page < MaxPages && offset + PageSize < cappedRecommendations.Count;

            return PartialView("_RecommendationBatch", new RecommendationBatchDto
            {
                Items = pageItems,
                FeedAction = "Feed",
                NextPage = hasMore ? page + 1 : null,
            });
        }
        catch
        {
            return PartialView("_RecommendationBatch", new RecommendationBatchDto
            {
                FeedAction = "Feed",
                ErrorMessage = "Could not load recommendations right now."
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ForYouFeed([FromQuery] int page = 1, CancellationToken cancellationToken = default)
    {
        if (page < 1 || page > MaxPages)
        {
            return PartialView("_RecommendationBatch", new RecommendationBatchDto
            {
                FeedAction = "ForYouFeed"
            });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return PartialView("_RecommendationBatch", new RecommendationBatchDto
            {
                FeedAction = "ForYouFeed",
                InfoMessage = "Login to see your personalized recommendations."
            });
        }

        try
        {
            var rankedTrackIds = await RankCollaborativeTrackIdsAsync(userId, cancellationToken);
            if (rankedTrackIds.Count == 0)
            {
                return PartialView("_RecommendationBatch", new RecommendationBatchDto
                {
                    FeedAction = "ForYouFeed",
                    InfoMessage = "Not enough listening overlap yet. Keep scrobbling to unlock For You recommendations."
                });
            }

            var cappedTrackIds = rankedTrackIds.Take(MaxRecommendations).ToList();
            var offset = (page - 1) * PageSize;
            var pageTrackIds = cappedTrackIds.Skip(offset).Take(PageSize).ToList();

            if (pageTrackIds.Count == 0)
            {
                return PartialView("_RecommendationBatch", new RecommendationBatchDto
                {
                    FeedAction = "ForYouFeed"
                });
            }

            var tracks = await _context.Tracks
                .AsNoTracking()
                .Include(t => t.Artist)
                .Include(t => t.Collection)
                .Where(t => pageTrackIds.Contains(t.Id))
                .ToListAsync(cancellationToken);

            var byId = tracks.ToDictionary(t => t.Id);
            var items = pageTrackIds
                .Where(byId.ContainsKey)
                .Select(trackId => MapTrackToRecommendation(byId[trackId]))
                .ToList();

            var hasMore = page < MaxPages && offset + PageSize < cappedTrackIds.Count;
            return PartialView("_RecommendationBatch", new RecommendationBatchDto
            {
                FeedAction = "ForYouFeed",
                Items = items,
                NextPage = hasMore ? page + 1 : null,
            });
        }
        catch
        {
            return PartialView("_RecommendationBatch", new RecommendationBatchDto
            {
                FeedAction = "ForYouFeed",
                ErrorMessage = "Could not build personalized recommendations right now."
            });
        }
    }

    private async Task<List<int>> RankCollaborativeTrackIdsAsync(string currentUserId, CancellationToken cancellationToken)
    {
        var currentTrackIds = await _context.Scrobbles
            .AsNoTracking()
            .Where(s => s.UserId == currentUserId)
            .Select(s => s.TrackId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (currentTrackIds.Count == 0)
        {
            return [];
        }

        var currentTrackIdSet = currentTrackIds.ToHashSet();

        var neighborUserIds = await _context.Scrobbles
            .AsNoTracking()
            .Where(s => s.UserId != currentUserId && currentTrackIdSet.Contains(s.TrackId))
            .Select(s => s.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (neighborUserIds.Count == 0)
        {
            return [];
        }

        var neighborRows = await _context.Scrobbles
            .AsNoTracking()
            .Where(s => neighborUserIds.Contains(s.UserId))
            .Select(s => new { s.UserId, s.TrackId })
            .Distinct()
            .ToListAsync(cancellationToken);

        if (neighborRows.Count == 0)
        {
            return [];
        }

        var userTotalCount = neighborRows
            .GroupBy(r => r.UserId)
            .ToDictionary(g => g.Key, g => g.Count());

        var userOverlapCount = neighborRows
            .Where(r => currentTrackIdSet.Contains(r.TrackId))
            .GroupBy(r => r.UserId)
            .ToDictionary(g => g.Key, g => g.Count());

        var currentCount = currentTrackIdSet.Count;
        if (currentCount == 0)
        {
            return [];
        }

        var similarityByUser = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var userId in neighborUserIds)
        {
            if (!userTotalCount.TryGetValue(userId, out var totalCount) || totalCount <= 0)
            {
                continue;
            }

            var overlapCount = userOverlapCount.GetValueOrDefault(userId, 0);
            if (overlapCount <= 0)
            {
                continue;
            }

            var similarity = overlapCount / Math.Sqrt(currentCount * totalCount);
            if (similarity > 0)
            {
                similarityByUser[userId] = similarity;
            }
        }

        if (similarityByUser.Count == 0)
        {
            return [];
        }

        var scoreByTrack = new Dictionary<int, double>();
        foreach (var row in neighborRows)
        {
            if (currentTrackIdSet.Contains(row.TrackId))
            {
                continue;
            }

            if (!similarityByUser.TryGetValue(row.UserId, out var similarity))
            {
                continue;
            }

            if (!scoreByTrack.TryAdd(row.TrackId, similarity))
            {
                scoreByTrack[row.TrackId] += similarity;
            }
        }

        if (scoreByTrack.Count == 0)
        {
            return [];
        }

        return scoreByTrack
            .OrderByDescending(pair => pair.Value)
            .Select(pair => pair.Key)
            .ToList();
    }

    private static RecommendationDto MapTrackToRecommendation(Models.Track track)
    {
        var title = string.IsNullOrWhiteSpace(track.TrackName)
            ? (string.IsNullOrWhiteSpace(track.FileName) ? "Unknown title" : track.FileName)
            : track.TrackName;

        var artist = string.IsNullOrWhiteSpace(track.Artist?.ArtistName)
            ? "Unknown artist"
            : track.Artist!.ArtistName;

        var appleMusicUrl = BuildAppleMusicUrl(track);

        return new RecommendationDto
        {
            Title = title,
            Artist = artist,
            ArtworkUrl = track.ArtworkUrl ?? string.Empty,
            AppleMusicUrl = appleMusicUrl,
            PreviewUrl = track.PreviewUrl ?? string.Empty,
        };
    }

    private static string BuildAppleMusicUrl(Models.Track track)
    {
        if (track.ItunesTrackId <= 0)
        {
            return string.Empty;
        }

        var itunesCollectionId = track.Collection?.ItunesCollectionId ?? 0;
        if (itunesCollectionId > 0)
        {
            return $"https://music.apple.com/us/album/id{itunesCollectionId}?i={track.ItunesTrackId}";
        }

        // Old iTunes URL format reliably redirects to Apple Music web.
        return $"https://itunes.apple.com/us/song/id{track.ItunesTrackId}";
    }

    private async Task<List<RecommendationDto>> LoadRecommendationsAsync(CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient();

        await using var stream = await client.GetStreamAsync(ItunesFeedUrl, cancellationToken);
        var feed = await JsonSerializer.DeserializeAsync<ItunesFeedResponse>(
            stream,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            },
            cancellationToken);

        var results = feed?.Feed?.Results;
        if (results is null || results.Count == 0)
        {
            return [];
        }

        return results
            .Select(item => new RecommendationDto
            {
                Title = item.Name?.Trim() ?? string.Empty,
                Artist = item.ArtistName?.Trim() ?? string.Empty,
                ArtworkUrl = item.ArtworkUrl100?.Trim() ?? string.Empty,
                AppleMusicUrl = item.Url?.Trim() ?? string.Empty,
                PreviewUrl = item.PreviewUrl?.Trim() ?? string.Empty,
            })
            .ToList();
    }

    private sealed class ItunesFeedResponse
    {
        public ItunesFeedData? Feed { get; init; }
    }

    private sealed class ItunesFeedData
    {
        public List<ItunesFeedItem> Results { get; init; } = [];
    }

    private sealed class ItunesFeedItem
    {
        public string? Name { get; init; }
        public string? ArtistName { get; init; }
        public string? ArtworkUrl100 { get; init; }
        public string? Url { get; init; }
        public string? PreviewUrl { get; init; }
    }
}
