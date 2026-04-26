namespace Hmqs.Api.Dtos;

public class ScrobbleResponseDto
{
    public int Id { get; set; }
    public Guid LocalTrackId { get; set; }
    public DateTime ScrobbleTime { get; set; }
    public int TrackDuration { get; set; }
    public int ListenedDuration { get; set; }
}
