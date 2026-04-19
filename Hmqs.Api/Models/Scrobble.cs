namespace Hmqs.Api.Models;

public class Scrobble
{
    public int Id { get; set; }
    public Guid ListenerId { get; set; }
    public Guid LocalTrackId { get; set; }
    public DateTime ScrobbleTime { get; set; }
    public int TrackDuration { get; set; }
    public int ListenedDuration { get; set; }

    public virtual Listener Listener { get; set; } = null!;
    public virtual LocalTrack LocalTrack { get; set; } = null!;
}
