namespace WebApp.Models;

public class UserArtistStat
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int ArtistId { get; set; }
    public int IgnoredCount { get; set; }
}
