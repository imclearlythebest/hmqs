namespace WebApp.Models;

public class SharedPlaylistTrack
{
    public int Id { get; set; }
    public string SharedPlaylistId { get; set; } = string.Empty;
    public SharedPlaylist? SharedPlaylist { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string Album { get; set; } = string.Empty;
    public int ItunesTrackId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
}
