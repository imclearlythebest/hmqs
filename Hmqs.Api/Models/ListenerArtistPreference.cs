namespace Hmqs.Api.Models;

public class ListenerArtistPreference
{
    public Guid ListenerId { get; set; }
    public Guid ArtistId { get; set; }
    public int DeprioritizedByEthicalCount { get; set; }

    public virtual Listener Listener { get; set; } = null!;
    public virtual Artist Artist { get; set; } = null!;
}