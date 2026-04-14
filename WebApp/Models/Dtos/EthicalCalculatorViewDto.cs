namespace WebApp.Models.Dtos;

public sealed class EthicalCalculatorViewDto
{
    public decimal MonthlyBudget { get; init; }
    public IReadOnlyList<EthicalArtistScoreDto> Artists { get; init; } = [];
}
