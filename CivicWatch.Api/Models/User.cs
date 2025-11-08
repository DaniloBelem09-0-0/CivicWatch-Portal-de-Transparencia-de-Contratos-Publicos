using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CivicWatch.Api.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        [Required, StringLength(100)]
        public required string Username { get; set; }
        public required byte[] PasswordHash { get; set; }
        public required byte[] PasswordSalt { get; set; }
        
        [ForeignKey("Role")]
        public int RoleId { get; set; }
        public required Role Role { get; set; }
        
        // Relacionamento 1:1
        public required UserProfile UserProfile { get; set; } 
    }
}