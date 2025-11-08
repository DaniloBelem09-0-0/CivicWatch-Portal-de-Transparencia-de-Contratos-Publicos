using System.ComponentModel.DataAnnotations;

namespace CivicWatch.Api.DTOs
{
    public class RegisterDto
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;
        
        [Required]
        public string NomeCompleto { get; set; } = string.Empty;

        [EmailAddress]
        public string? Email { get; set; }
    }
}