namespace WebApp.Models;
public class Track
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int ItunesTrackId { get; set; }
    public string TrackName { get; set; } = string.Empty;
    public int Year { get; set; }
    public string PreviewUrl { get; set; } = string.Empty;
    public string ArtworkUrl { get; set; } = string.Empty;
    public DateTime? LastCrawledAtUtc { get; set; }

    public int? ArtistId { get; set; }
    public Artist? Artist { get; set; }

    public string? GenreName { get; set; }
    public Genre? Genre { get; set; }

    public int? CollectionId { get; set; }
    public Collection? Collection { get; set; }
}