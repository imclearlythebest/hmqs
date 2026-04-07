using System.ComponentModel.DataAnnotations;

namespace WebApp.Models.Dtos;

public class RegisterDto
{
    [Required]
    public string Username { get; set; } = string.Empty;
    [EmailAddress]
    [Required]
    public string Email { get; set; } = string.Empty;
    [Required]
    public string Password { get; set; } = string.Empty;
    [Compare("Password")]
    [Required]
    public string ConfirmPassword { get; set; } = string.Empty;

}