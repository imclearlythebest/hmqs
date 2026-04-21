using System.Text.Json;

namespace WebApp.Services;

public class OdesliService(IHttpClientFactory httpClientFactory)
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public async Task<string?> GetAmazonStoreLinkAsync(long itunesId)
    {
        return await GetAmazonStoreLinkForSongAsync(itunesId);
    }

    public async Task<string?> GetAmazonStoreLinkForSongAsync(long itunesSongId)
    {
        return await GetAmazonStoreLinkByTypeAsync(itunesSongId, "song");
    }

    public async Task<string?> GetAmazonStoreLinkForAlbumAsync(long itunesAlbumId)
    {
        var albumLink = await GetAmazonStoreLinkByTypeAsync(itunesAlbumId, "album");
        if (!string.IsNullOrWhiteSpace(albumLink))
        {
            return albumLink;
        }

        // Fallback for inconsistent upstream mapping.
        return await GetAmazonStoreLinkByTypeAsync(itunesAlbumId, "song");
    }

    private async Task<string?> GetAmazonStoreLinkByTypeAsync(long itunesId, string entityType)
    {
        var client = _httpClientFactory.CreateClient();
        var url = $"https://api.song.link/v1-alpha.1/links?platform=itunes&type={entityType}&id={itunesId}";
        
        try
        {
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            return TryGetAmazonLink(doc.RootElement);
        }
        catch
        {
            // Fallback or log error
        }

        return null;
    }

    private static string? TryGetAmazonLink(JsonElement root)
    {
        if (root.TryGetProperty("linksByPlatform", out var links) &&
            links.TryGetProperty("amazonStore", out var amazonStore) &&
            amazonStore.TryGetProperty("url", out var urlElement))
        {
            return urlElement.GetString();
        }

        return null;
    }

    public async Task<string?> GetYoutubeMusicLinkAsync(long itunesId)
    {
        var client = _httpClientFactory.CreateClient();
        var url = $"https://api.song.link/v1-alpha.1/links?url=https://itunes.apple.com/us/song/id{itunesId}&userCountry=US";
        
        try
        {
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("linksByPlatform", out var links))
            {
                return null;
            }

            // Odesli can return different YouTube keys depending on availability.
            foreach (var key in new[] { "youtubeMusic", "youtube", "youtubeContent" })
            {
                if (links.TryGetProperty(key, out var platform) &&
                    platform.TryGetProperty("url", out var urlElement))
                {
                    var candidate = urlElement.GetString();
                    if (!string.IsNullOrWhiteSpace(candidate))
                    {
                        return candidate;
                    }
                }
            }
        }
        catch
        {
        }

        return null;
    }
}
