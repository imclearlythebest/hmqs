using System.ComponentModel.DataAnnotations;

namespace WebApp.Models.Dtos;
public class ScrobbleDto
{
    public int ItunesTrackId { get; set; }

    public int? ItunesArtistId { get; set; }

    public int? ItunesCollectionId { get; set; }

    [Range(0, 100, ErrorMessage = "Progress must be between 0 and 100")]
    public decimal Progress { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Duration must be non-negative")]
    public int DurationSeconds { get; set; }
}