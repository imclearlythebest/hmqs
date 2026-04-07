namespace HMQS.API.Models
{
    public class Song
    {
        public int Id { get; set; }

        // Which user owns this song in their library
        public int UserId { get; set; }

        // Album is optional - a song might not be matched to an album yet
        public int? AlbumId { get; set; }

        public string Title { get; set; } = string.Empty;

        // Song length in seconds (e.g. 214 = 3 min 34 sec)
        public int? Duration { get; set; }

        // Full path to the audio file on the user's machine
        // Example: C:\Music\Artist\Album\song.mp3
        public string? FilePath { get; set; }

        // Cover art - can be local path or URL from API
        public string? CoverArtUrl { get; set; }

        public string? MusicBrainzId { get; set; }

        // When the user added this song to HMQS
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public User User { get; set; } = null!;

        // Album is nullable because the song might not be linked yet
        public Album? Album { get; set; }

        public ICollection<PlayHistory> PlayHistories { get; set; } = new List<PlayHistory>();
    }
}