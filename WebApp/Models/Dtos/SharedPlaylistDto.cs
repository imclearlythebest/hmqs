namespace WebApp.Models.Dtos;

public class SharedPlaylistDto
{
    public string Name { get; set; } = string.Empty;
    public List<SharedPlaylistTrackDto> Tracks { get; set; } = new();
}

public class SharedPlaylistTrackDto
{
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string Album { get; set; } = string.Empty;
    public int ItunesTrackId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
}
