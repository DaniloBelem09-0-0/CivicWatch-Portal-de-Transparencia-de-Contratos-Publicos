using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;

namespace CivicWatch.Api.Models
{
    public class RespostaAlerta
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public DateTime DataResposta { get; set; } = DateTime.UtcNow;

        [Required]
        public required string Justificativa { get; set; }

        // FK do Alerta
        public int AlertaId { get; set; }
        public required Alerta Alerta { get; set; }

        // FK do Usuário
        public int UserId { get; set; }
        public required User User { get; set; }
    }
}