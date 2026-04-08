namespace WebApp.Models.Dtos;

class PlaylistDto
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public List<string> TrackFileNames { get; set; } = [];
}