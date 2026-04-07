using System.ComponentModel.DataAnnotations;

namespace WebApp.Models;
public class Genre
{
    [Key]
    public string GenreName { get; set; } = string.Empty;
    public int ItunesGenreId { get; set; }
}