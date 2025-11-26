using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace CivicWatch.Api.Models
{
    public class StatusAlerta
    {
        [Key]
        public int Id { get; set; }
        
        [Required, StringLength(50)]
        public required string Nome { get; set; }
        
        [StringLength(10)]
        public string CorHex { get; set; } = "";

        public ICollection<Alerta> Alertas { get; set; } = new List<Alerta>();
    }
}