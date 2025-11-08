using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace CivicWatch.Api.Models
{
    public class Fornecedor
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(18)]
        public required string Documento { get; set; } // CNPJ ou CPF

        [Required, StringLength(255)]
        public required string RazaoSocial { get; set; }

        [StringLength(50)]
        public string? StatusReceita { get; set; }

        // Relacionamento 1:1 Opcional
        public CheckIntegridade? CheckIntegridade { get; set; } 

        public ICollection<Contrato> Contratos { get; set; } = new List<Contrato>();
        public ICollection<Despesa> Despesas { get; set; } = new List<Despesa>();
    }
}