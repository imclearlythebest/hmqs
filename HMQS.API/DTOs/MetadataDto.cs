namespace HMQS.API.DTOs
{
    public class MetadataResultDto
    {
        // Stores iTunes track ID (we reuse this field name)
        public string MusicBrainzId { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string? ArtistName { get; set; }

        public string? AlbumTitle { get; set; }

        public int? ReleaseYear { get; set; }

        public int Score { get; set; }

        // New - iTunes provides cover art URLs directly
        public string? CoverArtUrl { get; set; }
    }

    public class ApplyMetadataDto
    {
        public int SongId { get; set; }

        public string MusicBrainzId { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string? ArtistName { get; set; }

        public string? AlbumTitle { get; set; }

        public int? ReleaseYear { get; set; }

        // New - save cover art URL to the song
        public string? CoverArtUrl { get; set; }
    }
}