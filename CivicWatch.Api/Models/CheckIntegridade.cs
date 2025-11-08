using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;

namespace CivicWatch.Api.Models
{
    public class CheckIntegridade
    {
        [Key]
        public int Id { get; set; }

        // Chave Estrangeira 1:1
        [ForeignKey("Fornecedor")]
        public int FornecedorId { get; set; }
        public required Fornecedor Fornecedor { get; set; }

        public DateTime DataUltimaVerificacao { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string? StatusReceitaWs { get; set; }
        
        public bool EmConformidade { get; set; }
    }
}