namespace WebApp.Models;
public class Scrobble
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public WebAppUser User { get; set; } = null!;

    public int TrackId { get; set; }
    public Track Track { get; set; } = null!;

    public DateTime ScrobbledAt { get; set; } = DateTime.UtcNow;
    public decimal Progress { get; set; } = 0m;
    public int DurationSeconds { get; set; }
}