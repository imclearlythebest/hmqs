using System.ComponentModel.DataAnnotations;

namespace HMQS.API.DTOs
{
    // This is the data a user sends when logging in
    public class LoginDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}