namespace WebApp.Models.ViewModels;

public class TrackShareViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string Album { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public Dictionary<string, string> PlatformUrls { get; set; } = new();
}

public class PlaylistShareViewModel
{
    public string Name { get; set; } = string.Empty;
    public List<TrackShareViewModel> Tracks { get; set; } = new();
}
