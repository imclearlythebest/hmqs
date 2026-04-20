namespace Hmqs.Api.Models;

public class LocalTrack
{
    public Guid Id { get; set; }
    public string? FileName { get; set; }
    public string? TrackTitle { get; set; }
    public string? Artist { get; set; }
    public string? Album { get; set; }
    public string? Genre { get; set; }
    public int? Year { get; set; }
    public string? ArtworkUrl { get; set; }

    public Guid ListenerId { get; set; }
    public Guid? GlobalTrackId { get; set; }

    public virtual Listener Listener { get; set; } = null!;
    public virtual GlobalTrack? GlobalTrack { get; set; }
    public virtual ICollection<Scrobble> Scrobbles { get; set; }
    public virtual ICollection<PlaylistTrack> PlaylistTracks { get; set; }

    public LocalTrack()
    {
        Scrobbles = new HashSet<Scrobble>();
        PlaylistTracks = new HashSet<PlaylistTrack>();
    }
}