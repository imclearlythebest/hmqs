namespace Hmqs.Api.Dtos;

public class GlobalTrackMatchDto
{
    public int ExternalTrackId { get; set; }
    public int ArtistExternalId { get; set; }
    public int AlbumExternalId { get; set; }
    public string TrackTitle { get; set; } = null!;
    public string ArtistName { get; set; } = null!;
    public string AlbumTitle { get; set; } = null!;
    public string? Genre { get; set; }
    public int? Year { get; set; }
    public string? ArtworkUrl { get; set; }
}
