namespace WebApp.Models;

public class Collection
{
    public int Id { get; set; }
    public int ItunesCollectionId { get; set; }
    public Artist Artist { get; set; } = new Artist();
    public Genre Genre { get; set; } = new Genre();
    public List<Track> Tracks { get; set; } = [];
}