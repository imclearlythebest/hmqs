namespace HMQS.API.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }

        // Foreign key - links this token to a specific user
        public int UserId { get; set; }

        // The random secure string sent to the client
        public string Token { get; set; } = string.Empty;

        // After this time, the token is invalid
        public DateTime ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property back to the User
        // EF Core uses UserId + this to build the JOIN
        public User User { get; set; } = null!;
    }
}