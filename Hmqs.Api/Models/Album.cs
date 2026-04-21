namespace Hmqs.Api.Models;

public class Album
{
    public Guid Id { get; set; }
    public int ExternalId { get; set; }
    public string Title { get; set; } = null!;
    public int? Year { get; set; }
    public string? PrimaryGenre { get; set; }
    public string? CoverArt { get; set; }
    public decimal Price { get; set; }
    
    public Guid ArtistId { get; set; }
    
    public virtual Artist Artist { get; set; } = null!;
    public virtual ICollection<GlobalTrack> GlobalTracks { get; set; }
    
    public Album()
    {
        GlobalTracks = new HashSet<GlobalTrack>();
    }
}
