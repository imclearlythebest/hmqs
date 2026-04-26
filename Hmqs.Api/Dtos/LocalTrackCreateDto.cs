namespace Hmqs.Api.Dtos;

public class LocalTrackCreateDto
{
    public string? FileName { get; set; }
    public string? TrackTitle { get; set; }
    public string? Artist { get; set; }
    public string? Album { get; set; }
    public string? Genre { get; set; }
    public int? Year { get; set; }
    public string? ArtworkUrl { get; set; }
}
