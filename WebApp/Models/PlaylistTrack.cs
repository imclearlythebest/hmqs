namespace WebApp.Models;

public class PlaylistTrack
{
    public int Id { get; set; }

    public int PlaylistId { get; set; }
    public Playlist Playlist { get; set; } = null!;

    public int TrackId { get; set; }
    public Track Track { get; set; } = null!;

    public int Order { get; set; }
}
