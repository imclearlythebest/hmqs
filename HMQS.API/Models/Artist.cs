namespace HMQS.API.Models
{
    public class Artist
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        // ID from MusicBrainz API - used for metadata lookups
        public string? MusicBrainzId { get; set; }

        // ID from Discogs API - used for vinyl/purchase data
        public string? DiscogsId { get; set; }

        // Short artist bio pulled from the API
        public string? Bio { get; set; }

        // One artist can have many albums
        public ICollection<Album> Albums { get; set; } = new List<Album>();
    }
}