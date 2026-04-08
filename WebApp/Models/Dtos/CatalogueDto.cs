namespace WebApp.Models.Dtos;

class CatalogueDto
{
    public int ItunesTrackId { get; set; }
    public string TrackTitle { get; set; } = "";
    public int ItunesArtistId { get; set; }
    public string Artist { get; set; } = "";
    public int ItunesCollectionId { get; set; }
    public string Album { get; set; } = "";
    public string Genre { get; set; } = "";
    public string ImageUrl { get; set; } = "";
}