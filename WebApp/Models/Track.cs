namespace WebApp.Models;
public class Track
{
    public int Id { get; set; }
    public int ItunesTrackId { get; set; }
    public string TrackName { get; set; } = string.Empty;
    public string PreviewUrl { get; set; } = string.Empty;
    public string ArtworkUrl { get; set; } = string.Empty;
    public Artist Artist { get; set; } = new Artist();
    public Genre Genre { get; set; } = new Genre();
}