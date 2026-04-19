namespace Hmqs.Api.Models;

public class GlobalTrack
{
    public Guid Id { get; set; }

    public string? TrackTitle { get; set; }
    public string? Artist { get; set; }
    public string? Album { get; set; }
    public string? Genre { get; set; }
    public int? Year { get; set; }
    public string? ArtworkUrl { get; set; }

    public virtual ICollection<LocalTrack> LocalTracks { get; set; }

    public GlobalTrack()
    {
        LocalTracks = new HashSet<LocalTrack>();
    }
}