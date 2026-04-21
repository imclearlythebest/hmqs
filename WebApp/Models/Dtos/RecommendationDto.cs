namespace WebApp.Models.Dtos;

public sealed class RecommendationDto
{
    public int ItunesTrackId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Artist { get; init; } = string.Empty;
    public string ArtworkUrl { get; init; } = string.Empty;
    public string AppleMusicUrl { get; init; } = string.Empty;
    public string PreviewUrl { get; init; } = string.Empty;
}
