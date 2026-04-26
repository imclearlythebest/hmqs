namespace WebApp.Models;

public class Playlist
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;
    public WebAppUser User { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<PlaylistTrack> PlaylistTracks { get; set; } = new();
}
