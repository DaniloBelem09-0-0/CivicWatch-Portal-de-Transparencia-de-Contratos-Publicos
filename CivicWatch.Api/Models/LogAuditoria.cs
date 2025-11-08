using System.ComponentModel.DataAnnotations;
using System;

namespace CivicWatch.Api.Models
{
    public class LogAuditoria
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public DateTime DataAcao { get; set; } = DateTime.UtcNow;

        [Required]
        public required string Acao { get; set; }

        public string? Detalhes { get; set; }

        public int UserId { get; set; }
        public required User User { get; set; }
    }
}