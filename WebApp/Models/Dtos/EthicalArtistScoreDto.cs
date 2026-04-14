namespace WebApp.Models.Dtos;

public sealed class EthicalArtistScoreDto
{
    public int ArtistId { get; init; }
    public string ArtistName { get; init; } = string.Empty;
    public int UserListenedSeconds { get; init; }
    public int GlobalListenedSeconds { get; init; }
    public int IgnoredCount { get; init; }
    public decimal InversePopularity { get; init; }
    public decimal ListeningShare { get; init; }
    public decimal IgnoredShare { get; init; }
    public decimal EthicalScore { get; init; }
    public decimal SuggestedBudget { get; init; }
}
