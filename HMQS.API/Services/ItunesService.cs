using System.Text.Json;
using HMQS.API.DTOs;

namespace HMQS.API.Services
{
    public class ItunesService
    {
        private readonly HttpClient _http;
        private readonly ILogger<ItunesService> _logger;

        public ItunesService(HttpClient http, ILogger<ItunesService> logger)
        {
            _http = http;
            _logger = logger;
        }

        // Search iTunes for tracks matching a query string
        // Example query: "Billie Jean Michael Jackson"
        public async Task<List<MetadataResultDto>> SearchAsync(string query)
        {
            try
            {
                // iTunes search endpoint
                // media=music limits results to music only
                // limit=5 returns top 5 matches
                var url = $"search?term={Uri.EscapeDataString(query)}&media=music&limit=5";

                var response = await _http.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("iTunes API returned {StatusCode}", response.StatusCode);
                    return new List<MetadataResultDto>();
                }

                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);
                var results = new List<MetadataResultDto>();

                // iTunes returns: { "resultCount": 5, "results": [ {...}, {...} ] }
                if (!doc.RootElement.TryGetProperty("results", out var tracks))
                    return results;

                int score = 100; // iTunes does not return a score so we simulate it
                                 // First result gets 100, second 90, and so on

                foreach (var track in tracks.EnumerateArray())
                {
                    var result = new MetadataResultDto();

                    // iTunes track ID used as our external reference
                    if (track.TryGetProperty("trackId", out var trackId))
                        result.MusicBrainzId = trackId.GetInt64().ToString();
                    // We reuse the MusicBrainzId field to store the iTunes track ID

                    // Track title
                    if (track.TryGetProperty("trackName", out var trackName))
                        result.Title = trackName.GetString() ?? string.Empty;

                    // Artist name
                    if (track.TryGetProperty("artistName", out var artistName))
                        result.ArtistName = artistName.GetString();

                    // Album title (called "collectionName" in iTunes)
                    if (track.TryGetProperty("collectionName", out var collectionName))
                        result.AlbumTitle = collectionName.GetString();

                    // Release year - iTunes returns full date like "2001-10-25T07:00:00Z"
                    if (track.TryGetProperty("releaseDate", out var releaseDate))
                    {
                        var dateStr = releaseDate.GetString();
                        if (!string.IsNullOrEmpty(dateStr) && dateStr.Length >= 4)
                        {
                            if (int.TryParse(dateStr[..4], out var year))
                                result.ReleaseYear = year;
                        }
                    }

                    // Cover art URL - iTunes returns 100x100 by default
                    // Replace "100x100" with "600x600" for higher resolution
                    if (track.TryGetProperty("artworkUrl100", out var artworkUrl))
                    {
                        var url100 = artworkUrl.GetString() ?? string.Empty;
                        result.CoverArtUrl = url100.Replace("100x100", "600x600");
                    }

                    // Simulate descending score since iTunes returns best match first
                    result.Score = score;
                    score = Math.Max(score - 10, 50);

                    results.Add(result);
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "iTunes search failed for query: {Query}", query);
                return new List<MetadataResultDto>();
            }
        }
    }
}