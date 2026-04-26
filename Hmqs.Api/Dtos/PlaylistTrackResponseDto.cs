namespace Hmqs.Api.Dtos;

public class PlaylistTrackResponseDto
{
    public int Id { get; set; }
    public Guid LocalTrackId { get; set; }
    public int Position { get; set; }
    public DateTime AddedAt { get; set; }
}
