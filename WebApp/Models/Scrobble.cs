namespace WebApp.Models;
public class Scrobble
{
    public int Id { get; set; }
    public WebAppUser? User { get; set; }
    public Track Track { get; set; } = new Track();
    public DateTime ScrobbledAt { get; set; } = DateTime.Now;
    public decimal Progress { get; set; } = 0m;
    public int DurationSeconds { get; set; }
}