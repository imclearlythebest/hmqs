namespace WebApp.Models;

public class Collection
{
    public int Id { get; set; }
    public int ItunesCollectionId { get; set; }
    public DateTime? LastCrawledAtUtc { get; set; }

    public int? ArtistId { get; set; }
    public Artist? Artist { get; set; }

    public string? GenreName { get; set; }
    public Genre? Genre { get; set; }

    public List<Track> Tracks { get; set; } = [];
}