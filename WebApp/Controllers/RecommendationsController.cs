using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using WebApp.Models.Dtos;

namespace WebApp.Controllers;

public class RecommendationsController(IHttpClientFactory httpClientFactory) : Controller
{
    private const int PageSize = 10;
    private const int MaxPages = 5;
    private const int MaxRecommendations = PageSize * MaxPages;
    private const string ItunesFeedUrl = "https://rss.marketingtools.apple.com/api/v2/us/music/most-played/100/songs.json";

    [HttpGet]
    public IActionResult Index() => View();

    [HttpGet]
    public async Task<IActionResult> Feed([FromQuery] int page = 1, CancellationToken cancellationToken = default)
    {
        if (page < 1 || page > MaxPages)
        {
            return PartialView("_RecommendationBatch", new RecommendationBatchDto());
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
                NextPage = hasMore ? page + 1 : null,
            });
        }
        catch
        {
            return PartialView("_RecommendationBatch", new RecommendationBatchDto
            {
                ErrorMessage = "Could not load recommendations right now."
            });
        }
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
