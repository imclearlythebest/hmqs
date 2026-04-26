namespace Hmqs.Api.Models;

public class GlobalTrack
{
    public Guid Id { get; set; }
    public int ExternalId { get; set; }

    public string? TrackTitle { get; set; }
    public string? Genre { get; set; }
    public int? Year { get; set; }
    public string? ArtworkUrl { get; set; }
    public Guid? ArtistId { get; set; }
    public Guid? AlbumId { get; set; }
    public virtual Artist? Artist { get; set; }
    public virtual Album? Album { get; set; }
    public virtual ICollection<LocalTrack> LocalTracks { get; set; }

    public GlobalTrack()
    {
        LocalTracks = new HashSet<LocalTrack>();
    }
}