namespace Hmqs.Api.Dtos;

public sealed class EthicalDummyPurchaseResultDto
{
    public string Message { get; init; } = string.Empty;
    public int AffectedArtistsCount { get; init; }
    public EthicalCalculatorResultDto Calculator { get; init; } = new();
}