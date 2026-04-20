using Microsoft.AspNetCore.Identity;
namespace Hmqs.Api.Models;

public class Listener: IdentityUser<Guid>
{
    public virtual ICollection<LocalTrack> LocalTracks { get; set; }
    public virtual ICollection<GlobalTrack> GlobalTracks { get; set; }
    public virtual ICollection<Scrobble> Scrobbles { get; set; }
    public virtual ICollection<Playlist> Playlists { get; set; }
    public Listener()
    {
        LocalTracks = new HashSet<LocalTrack>();
        GlobalTracks = new HashSet<GlobalTrack>();
        Scrobbles = new HashSet<Scrobble>();
        Playlists = new HashSet<Playlist>();
    }
}
