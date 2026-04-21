namespace WebApp.Models;

public class UserActivity
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public virtual WebAppUser User { get; set; }
    public string? TrackTitle { get; set; }
    public string? ArtistName { get; set; }
    public string? ArtworkUrl { get; set; }
    public int DurationSeconds { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
