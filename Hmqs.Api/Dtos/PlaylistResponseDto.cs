namespace Hmqs.Api.Dtos;

public class PlaylistResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public IEnumerable<PlaylistTrackResponseDto> Tracks { get; set; } = Array.Empty<PlaylistTrackResponseDto>();
}
