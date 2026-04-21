namespace Hmqs.Api.Dtos;

public sealed class EthicalDummyPurchaseRequestDto
{
    public Guid PurchasedArtistId { get; init; }
    public decimal MonthlyBudget { get; init; } = 500m;
}