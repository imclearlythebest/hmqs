namespace HMQS.API.Models
{
    public class User
    {
        // Primary key - EF Core auto-increments this
        public int Id { get; set; }

        // Unique display name for the user
        public string Username { get; set; } = string.Empty;

        // Used for login - must be unique
        public string Email { get; set; } = string.Empty;

        // We NEVER store plain passwords - only the bcrypt hash
        public string PasswordHash { get; set; } = string.Empty;

        // Automatically set when the account is created
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Has the user linked their Spotify account?
        public bool SpotifyConnected { get; set; } = false;

        // Navigation properties - EF Core uses these to understand relationships
        // One user can have many refresh tokens (multiple devices)
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

        // One user can have many songs in their library
        public ICollection<Song> Songs { get; set; } = new List<Song>();

        // One user can have many play history records
        public ICollection<PlayHistory> PlayHistories { get; set; } = new List<PlayHistory>();
    }
}