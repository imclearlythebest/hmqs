using Hmqs.Api.Data;
using Hmqs.Api.Dtos;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Hmqs.Api.Services;

public class RecommendationService
{
    private const int PageSize = 10;
    private const int MaxPages = 5;
    private const int MaxRecommendations = PageSize * MaxPages;
    private const string ItunesFeedUrl = "https://rss.marketingtools.apple.com/api/v2/us/music/most-played/100/songs.json";

    private readonly AppDbContext _context;
    private readonly HttpClient _httpClient;

    public RecommendationService(AppDbContext context, HttpClient httpClient)
    {
        _context = context;
        _httpClient = httpClient;
    }

    public async Task<RecommendationBatchDto> GetFeedAsync(int page, CancellationToken cancellationToken = default)
    {
        if (!IsValidPage(page))
        {
            return new RecommendationBatchDto
            {
                FeedAction = "Feed"
            };
        }

        try
        {
            var allRecommendations = await LoadRecommendationsAsync(cancellationToken);
            var cappedRecommendations = allRecommendations.Take(MaxRecommendations).ToList();
            var offset = (page - 1) * PageSize;
            var pageItems = cappedRecommendations.Skip(offset).Take(PageSize).ToList();
            var hasMore = page < MaxPages && offset + PageSize < cappedRecommendations.Count;

            return new RecommendationBatchDto
            {
                FeedAction = "Feed",
                Items = pageItems,
                NextPage = hasMore ? page + 1 : null
            };
        }
        catch
        {
            return new RecommendationBatchDto
            {
                FeedAction = "Feed",
                ErrorMessage = "Could not load recommendations right now."
            };
        }
    }

    public async Task<RecommendationBatchDto> GetForYouFeedAsync(Guid listenerId, int page, CancellationToken cancellationToken = default)
    {
        if (!IsValidPage(page))
        {
            return new RecommendationBatchDto
            {
                FeedAction = "ForYouFeed"
            };
        }

        try
        {
            var rankedTrackIds = await RankCollaborativeTrackIdsAsync(listenerId, cancellationToken);
            if (rankedTrackIds.Count == 0)
            {
                return new RecommendationBatchDto
                {
                    FeedAction = "ForYouFeed",
                    InfoMessage = "Not enough listening overlap yet. Keep scrobbling to unlock For You recommendations."
                };
            }

            var cappedTrackIds = rankedTrackIds.Take(MaxRecommendations).ToList();
            var offset = (page - 1) * PageSize;
            var pageTrackIds = cappedTrackIds.Skip(offset).Take(PageSize).ToList();

            if (pageTrackIds.Count == 0)
            {
                return new RecommendationBatchDto
                {
                    FeedAction = "ForYouFeed"
                };
            }

            var tracks = await _context.GlobalTracks
                .AsNoTracking()
                .Include(t => t.Artist)
                .Include(t => t.Album)
                .Where(t => pageTrackIds.Contains(t.Id))
                .ToListAsync(cancellationToken);

            var byId = tracks.ToDictionary(t => t.Id);
            var items = pageTrackIds
                .Where(byId.ContainsKey)
                .Select(trackId => MapTrackToRecommendation(byId[trackId]))
                .ToList();

            var hasMore = page < MaxPages && offset + PageSize < cappedTrackIds.Count;
            return new RecommendationBatchDto
            {
                FeedAction = "ForYouFeed",
                Items = items,
                NextPage = hasMore ? page + 1 : null
            };
        }
        catch
        {
            return new RecommendationBatchDto
            {
                FeedAction = "ForYouFeed",
                ErrorMessage = "Could not build personalized recommendations right now."
            };
        }
    }

    private async Task<List<Guid>> RankCollaborativeTrackIdsAsync(Guid listenerId, CancellationToken cancellationToken)
    {
        var currentTrackIds = await _context.Scrobbles
            .AsNoTracking()
            .Where(s => s.ListenerId == listenerId)
            .Join(
                _context.LocalTracks.AsNoTracking().Where(t => t.GlobalTrackId.HasValue),
                scrobble => scrobble.LocalTrackId,
                track => track.Id,
                (scrobble, track) => track.GlobalTrackId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (currentTrackIds.Count == 0)
        {
            return [];
        }

        var currentTrackSet = currentTrackIds.ToHashSet();

        var neighborListenerIds = await _context.Scrobbles
            .AsNoTracking()
            .Where(s => s.ListenerId != listenerId)
            .Join(
                _context.LocalTracks.AsNoTracking().Where(t => t.GlobalTrackId.HasValue),
                scrobble => scrobble.LocalTrackId,
                track => track.Id,
                (scrobble, track) => new { scrobble.ListenerId, GlobalTrackId = track.GlobalTrackId!.Value })
            .Where(x => currentTrackSet.Contains(x.GlobalTrackId))
            .Select(x => x.ListenerId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (neighborListenerIds.Count == 0)
        {
            return [];
        }

        var neighborRows = await _context.Scrobbles
            .AsNoTracking()
            .Where(s => neighborListenerIds.Contains(s.ListenerId))
            .Join(
                _context.LocalTracks.AsNoTracking().Where(t => t.GlobalTrackId.HasValue),
                scrobble => scrobble.LocalTrackId,
                track => track.Id,
                (scrobble, track) => new { scrobble.ListenerId, GlobalTrackId = track.GlobalTrackId!.Value })
            .Distinct()
            .ToListAsync(cancellationToken);

        if (neighborRows.Count == 0)
        {
            return [];
        }

        var listenerTotalCount = neighborRows
            .GroupBy(x => x.ListenerId)
            .ToDictionary(group => group.Key, group => group.Count());

        var listenerOverlapCount = neighborRows
            .Where(x => currentTrackSet.Contains(x.GlobalTrackId))
            .GroupBy(x => x.ListenerId)
            .ToDictionary(group => group.Key, group => group.Count());

        var currentCount = currentTrackSet.Count;
        if (currentCount == 0)
        {
            return [];
        }

        var similarityByListener = new Dictionary<Guid, double>();
        foreach (var neighborId in neighborListenerIds)
        {
            if (!listenerTotalCount.TryGetValue(neighborId, out var totalCount) || totalCount <= 0)
            {
                continue;
            }

            var overlapCount = listenerOverlapCount.GetValueOrDefault(neighborId, 0);
            if (overlapCount <= 0)
            {
                continue;
            }

            var similarity = overlapCount / Math.Sqrt(currentCount * totalCount);
            if (similarity > 0)
            {
                similarityByListener[neighborId] = similarity;
            }
        }

        if (similarityByListener.Count == 0)
        {
            return [];
        }

        var scoreByTrack = new Dictionary<Guid, double>();
        foreach (var row in neighborRows)
        {
            if (currentTrackSet.Contains(row.GlobalTrackId))
            {
                continue;
            }

            if (!similarityByListener.TryGetValue(row.ListenerId, out var similarity))
            {
                continue;
            }

            if (!scoreByTrack.TryAdd(row.GlobalTrackId, similarity))
            {
                scoreByTrack[row.GlobalTrackId] += similarity;
            }
        }

        if (scoreByTrack.Count == 0)
        {
            return [];
        }

        return scoreByTrack
            .OrderByDescending(x => x.Value)
            .Select(x => x.Key)
            .ToList();
    }

    private async Task<List<RecommendationItemDto>> LoadRecommendationsAsync(CancellationToken cancellationToken)
    {
        await using var stream = await _httpClient.GetStreamAsync(ItunesFeedUrl, cancellationToken);
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
            .Select(item => new RecommendationItemDto
            {
                Title = item.Name?.Trim() ?? string.Empty,
                Artist = item.ArtistName?.Trim() ?? string.Empty,
                Album = item.CollectionName?.Trim() ?? string.Empty,
                ArtworkUrl = item.ArtworkUrl100?.Trim() ?? string.Empty,
            })
            .ToList();
    }

    private static RecommendationItemDto MapTrackToRecommendation(Models.GlobalTrack track)
    {
        return new RecommendationItemDto
        {
            Title = string.IsNullOrWhiteSpace(track.TrackTitle) ? "Unknown title" : track.TrackTitle,
            Artist = string.IsNullOrWhiteSpace(track.Artist?.Name) ? "Unknown artist" : track.Artist.Name,
            Album = string.IsNullOrWhiteSpace(track.Album?.Title) ? "Unknown album" : track.Album.Title,
            ArtworkUrl = track.ArtworkUrl ?? string.Empty,
        };
    }

    private static bool IsValidPage(int page)
    {
        return page >= 1 && page <= MaxPages;
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
        public string? CollectionName { get; init; }
        public string? ArtworkUrl100 { get; init; }
    }
}