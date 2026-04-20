namespace WebApp.Models;

public class SharedPlaylist
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<SharedPlaylistTrack> Tracks { get; set; } = new();
}
