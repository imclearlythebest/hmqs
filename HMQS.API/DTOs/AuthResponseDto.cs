namespace HMQS.API.DTOs
{
    // This is what the API sends BACK to the client after login or register
    // We never send back the password or password hash
    public class AuthResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;   // Short-lived JWT (15 min)
        public string RefreshToken { get; set; } = string.Empty;  // Long-lived token (7 days)
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}