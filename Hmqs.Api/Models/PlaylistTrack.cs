namespace Hmqs.Api.Models;

public class PlaylistTrack
{
    public int Id { get; set; }
    public Guid PlaylistId { get; set; }
    public Guid LocalTrackId { get; set; }
    public int Position { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    
    public virtual Playlist Playlist { get; set; } = null!;
    public virtual LocalTrack LocalTrack { get; set; } = null!;
}
