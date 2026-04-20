namespace Hmqs.Api.Models;

public class Playlist
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public Guid OwnerId { get; set; }
    
    public virtual Listener Owner { get; set; } = null!;
    public virtual ICollection<PlaylistTrack> PlaylistTracks { get; set; }
    
    public Playlist()
    {
        PlaylistTracks = new HashSet<PlaylistTrack>();
    }
}
