namespace Hmqs.Api.Dtos;

public sealed class RecommendationItemDto
{
    public string Title { get; init; } = string.Empty;
    public string Artist { get; init; } = string.Empty;
    public string Album { get; init; } = string.Empty;
    public string ArtworkUrl { get; init; } = string.Empty;
}