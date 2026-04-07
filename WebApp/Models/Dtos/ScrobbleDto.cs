using System.ComponentModel.DataAnnotations;

namespace WebApp.Models.Dtos;
public class ScrobbleDto
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Track ID must be greater than 0")]
    public int ItunesTrackId { get; set; }

    [Range(0, 100, ErrorMessage = "Progress must be between 0 and 100")]
    public decimal Progress { get; set; }
}