namespace Hmqs.Api.Dtos;

public sealed class EthicalArtistAllocationDto
{
    public Guid ArtistId { get; init; }
    public string ArtistName { get; init; } = string.Empty;
    public int ListenerListenedSeconds { get; init; }
    public int GlobalListenedSeconds { get; init; }
    public int DeprioritizedByEthicalCount { get; init; }
    public decimal InversePopularityScore { get; init; }
    public decimal ListeningShare { get; init; }
    public decimal DeprioritizationSignalShare { get; init; }
    public decimal EthicalScore { get; init; }
    public decimal SuggestedBudget { get; init; }
}