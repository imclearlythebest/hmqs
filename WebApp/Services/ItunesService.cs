using System.Text.Json;

namespace WebApp.Services;

public class ItunesService(IHttpClientFactory httpClientFactory)
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public async Task<(string? Name, long? Id, decimal? Price)> GetBestAlbumAsync(int? itunesArtistId, decimal targetBudget)
    {
        if (!itunesArtistId.HasValue || itunesArtistId <= 0) return (null, null, null);

        var client = _httpClientFactory.CreateClient();
        var url = $"https://itunes.apple.com/lookup?id={itunesArtistId}&entity=album";
        
        try
        {
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) return (null, null, null);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            
            var albums = doc.RootElement.GetProperty("results").EnumerateArray()
                .Where(r => r.GetProperty("wrapperType").GetString() == "collection" && 
                            r.GetProperty("collectionType").GetString() == "Album")
                .Select(r => new
                {
                    Name = r.GetProperty("collectionName").GetString(),
                    Id = r.GetProperty("collectionId").GetInt64(),
                    Price = TryGetDecimal(r, "collectionPrice")
                })
                .ToList();

            if (albums.Count == 0) return (null, null, null);

            var best = albums
                .Where(album => album.Price.HasValue)
                .OrderBy(album => Math.Abs(album.Price!.Value - targetBudget))
                .FirstOrDefault()
                ?? albums.First();

            return (best.Name, best.Id, best.Price);
        }
        catch
        {
            return (null, null, null);
        }
    }

    private static decimal? TryGetDecimal(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetDecimal(out var decimalValue))
        {
            return decimalValue;
        }

        if (property.ValueKind == JsonValueKind.String && decimal.TryParse(property.GetString(), out var parsedValue))
        {
            return parsedValue;
        }

        return null;
    }
}
