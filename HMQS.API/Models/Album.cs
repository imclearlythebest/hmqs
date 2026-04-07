namespace HMQS.API.Models
{
    public class Album
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        // Foreign key - which artist made this album
        public int ArtistId { get; set; }

        public int? ReleaseYear { get; set; }

        // URL to the album cover image (from MusicBrainz or Discogs)
        public string? CoverArtUrl { get; set; }

        public string? MusicBrainzId { get; set; }

        public string? DiscogsId { get; set; }

        // Navigation properties
        public Artist Artist { get; set; } = null!;

        public ICollection<Song> Songs { get; set; } = new List<Song>();
    }
}