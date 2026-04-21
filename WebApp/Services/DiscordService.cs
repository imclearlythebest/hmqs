using System.Text.Json;
using System.Text;

namespace WebApp.Services;

public class DiscordService(IHttpClientFactory httpClientFactory)
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public async Task PostNowPlayingAsync(string webhookUrl, string title, string artist, string? artworkUrl, string username)
    {
        if (string.IsNullOrWhiteSpace(webhookUrl)) return;

        var client = _httpClientFactory.CreateClient();
        
        var payload = new
        {
            embeds = new[]
            {
                new
                {
                    title = "🎶 Listening To",
                    description = $"**{title}**\nby {artist}",
                    color = 5814783, // Discord Blurple
                    thumbnail = !string.IsNullOrWhiteSpace(artworkUrl) ? new { url = artworkUrl } : null,
                    footer = new { text = $"Sent by {username} through HMQS" }
                }
            }
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            await client.PostAsync(webhookUrl, content);
        }
        catch
        {
            // Fail silently to avoid interrupting the main flow
        }
    }
}
