using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;

namespace CivicWatch.Api.Models
{
    public class Alerta
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public DateTime DataGeracao { get; set; } = DateTime.UtcNow;

        public string? DescricaoOcorrencia { get; set; }
        
        // FK da Regra violada
        public int RegraAlertaId { get; set; }
        public required RegraAlerta RegraAlerta { get; set; }

        // FK do Status atual
        public int StatusAlertaId { get; set; }
        public required StatusAlerta StatusAlerta { get; set; }

        // Opcional: Relacionar a uma Entidade (Contrato, Despesa, Fornecedor)
        public int? EntidadeRelacionadaId { get; set; } 
        public string? TipoEntidadeRelacionada { get; set; } 
    }
}