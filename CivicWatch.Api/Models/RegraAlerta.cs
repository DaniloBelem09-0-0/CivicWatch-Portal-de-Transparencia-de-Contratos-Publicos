using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace CivicWatch.Api.Models
{
    public class RegraAlerta
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public required string Nome { get; set; }

        [Required]
        public required string DescricaoLogica { get; set; }

        [Required]
        public bool Ativa { get; set; } = true; 

        public string? TipoEntidadeAfetada { get; set; }

        public ICollection<Alerta> Alertas { get; set; } = new List<Alerta>();
    }
}