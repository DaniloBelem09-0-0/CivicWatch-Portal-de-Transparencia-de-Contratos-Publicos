using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CivicWatch.Api.Models
{
    public class UserProfile
    {
        [Key]
        public int Id { get; set; }
        [Required, StringLength(150)]
        public required string NomeCompleto { get; set; }
        
        [EmailAddress, StringLength(100)]
        public string? Email { get; set; }
        
        // Chave Estrangeira 1:1
        [ForeignKey("User")]
        public int UserId { get; set; }
        public User? User { get; set; }
    }
}