namespace HMQS.API.Models
{
    public class PlayHistory
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int SongId { get; set; }

        // Exact time the user played this song
        public DateTime PlayedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public User User { get; set; } = null!;

        public Song Song { get; set; } = null!;
    }
}