namespace WebApp.Models.Dtos;

public sealed class RecommendationBatchDto
{
    public IReadOnlyList<RecommendationDto> Items { get; init; } = [];
    public int? NextPage { get; init; }
    public string FeedAction { get; init; } = "Feed";
    public string? InfoMessage { get; init; }
    public string? ErrorMessage { get; init; }
}
