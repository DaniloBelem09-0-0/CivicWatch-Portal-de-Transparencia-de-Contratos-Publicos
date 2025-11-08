using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace CivicWatch.Api.Models
{
    public class Role
    {
        [Key]
        public int Id { get; set; }
        [Required, StringLength(50)]
        public required string Nome { get; set; } 
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}