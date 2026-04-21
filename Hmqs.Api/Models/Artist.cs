namespace Hmqs.Api.Models;

public class Artist
{
    public Guid Id { get; set; }
    public int ExternalId { get; set; }
    public string Name { get; set; } = null!;
    public string? PrimaryGenre { get; set; }


    public virtual ICollection<GlobalTrack> GlobalTracks { get; set; }
    public virtual ICollection<Album> Albums { get; set; }
    
    public Artist()
    {
        GlobalTracks = new HashSet<GlobalTrack>();
        Albums = new HashSet<Album>();
    }
}
