using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;

namespace CivicWatch.Api.Models
{
    public class Denuncia
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public DateTime DataDenuncia { get; set; } = DateTime.UtcNow;

        [Required]
        public required string Conteudo { get; set; }

        public int UserId { get; set; }
        public required User User { get; set; }
        
        // Relacionamento Opcional com Alerta
        public int? AlertaId { get; set; }
        public Alerta? Alerta { get; set; }
    }
}