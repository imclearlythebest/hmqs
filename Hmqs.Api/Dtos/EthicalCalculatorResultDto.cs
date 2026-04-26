namespace Hmqs.Api.Dtos;

public sealed class EthicalCalculatorResultDto
{
    public decimal MonthlyBudget { get; init; }
    public IReadOnlyList<EthicalArtistAllocationDto> Artists { get; init; } = [];
}