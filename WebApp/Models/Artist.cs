using System.ComponentModel.DataAnnotations;

namespace WebApp.Models;
public class Artist
{
    public int Id { get; set; }
    public int ItunesArtistId { get; set; }
    [Required]
    public string ArtistName { get; set; } = string.Empty;
    public Genre PrimaryGenre { get; set; } = new Genre();
}