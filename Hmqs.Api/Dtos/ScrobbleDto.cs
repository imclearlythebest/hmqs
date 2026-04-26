namespace Hmqs.Api.Dtos;

public class ScrobbleDto
{
    public Guid LocalTrackId { get; set; }
    public DateTime ScrobbleTime { get; set; }
    public int TrackDuration { get; set; }
    public int ListenedDuration { get; set; }
}
