namespace HMQS.API.DTOs
{
    // What the client sends when starting an import
    public class SpotifyImportRequestDto
    {
        // List of track names the user copied from Spotify
        // Example: ["Billie Jean", "Bohemian Rhapsody", "Style"]
        public List<string> TrackNames { get; set; } = new List<string>();
    }

    // Result for a single track after import processing
    public class SpotifyImportResultDto
    {
        // The original Spotify track name the user pasted
        public string SpotifyTrackName { get; set; } = string.Empty;

        // "Matched" = found in local library
        // "NotFound" = not in local library yet
        public string Status { get; set; } = string.Empty;

        // If matched, this is the local song it was linked to
        public int? MatchedSongId { get; set; }
        public string? MatchedSongTitle { get; set; }
    }

    // Summary of the entire import job
    public class SpotifyImportSummaryDto
    {
        public int TotalTracks { get; set; }
        public int MatchedCount { get; set; }
        public int NotFoundCount { get; set; }
        public List<SpotifyImportResultDto> Results { get; set; } = new();
    }
}