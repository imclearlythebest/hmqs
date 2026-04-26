namespace WebApp.Models.Dtos;

public sealed class EthicalPurchaseRedirectViewDto
{
    public string ArtistName { get; init; } = string.Empty;
    public string AlbumName { get; init; } = string.Empty;
    public decimal? AlbumPrice { get; init; }
    public string AmazonStoreUrl { get; init; } = string.Empty;
    public decimal Budget { get; init; }
}