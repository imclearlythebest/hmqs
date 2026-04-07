namespace HMQS.API.Models
{
    public class SpotifyImport
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        // The original Spotify track ID (e.g. "3n3Ppam7vgaVa1iaRUIOKE")
        public string SpotifyTrackId { get; set; } = string.Empty;

        // If we found a match in the local library, link it here
        public int? MatchedSongId { get; set; }

        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

        // "Pending" = not yet searched
        // "Matched" = found a local file
        // "NotFound" = no match found
        public string Status { get; set; } = "Pending";

        // Navigation properties
        public User User { get; set; } = null!;

        public Song? MatchedSong { get; set; }
    }
}