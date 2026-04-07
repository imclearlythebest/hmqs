namespace HMQS.API.DTOs
{
    // What the client sends when adding a new song
    public class AddSongDto
    {
        public string Title { get; set; } = string.Empty;

        // Full path to the audio file on the user's machine
        // Example: C:\Music\Artist\song.mp3
        public string? FilePath { get; set; }

        // Optional - user may not know the album yet
        public int? AlbumId { get; set; }

        // Song length in seconds
        public int? Duration { get; set; }

        // Cover art URL from an external API or local path
        public string? CoverArtUrl { get; set; }
    }

    // What the API sends back when returning song data
    // We use a separate response DTO to control exactly what gets exposed
    public class SongResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? FilePath { get; set; }
        public int? Duration { get; set; }
        public string? CoverArtUrl { get; set; }
        public string? AlbumTitle { get; set; }   // From the joined Album
        public string? ArtistName { get; set; }   // From the joined Artist via Album
        public DateTime AddedAt { get; set; }

        // Helper: converts seconds to a readable format like "3:45"
        public string DurationFormatted => Duration.HasValue
            ? $"{Duration.Value / 60}:{(Duration.Value % 60):D2}"
            : "--:--";
    }
}