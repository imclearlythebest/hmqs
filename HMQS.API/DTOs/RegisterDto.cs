using System.ComponentModel.DataAnnotations;

namespace HMQS.API.DTOs
{
    // This is the data a user sends when creating an account
    // We only ask for what we need - nothing more
    public class RegisterDto
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress] // Validates that the format is a real email
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)] // Minimum 6 character password
        public string Password { get; set; } = string.Empty;
    }
}