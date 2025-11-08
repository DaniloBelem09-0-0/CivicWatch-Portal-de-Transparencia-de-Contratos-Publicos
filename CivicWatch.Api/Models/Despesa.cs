using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic;

namespace CivicWatch.Api.Models
{
    public class Despesa
    {
        [Key]
        public int Id { get; set; }
        
        [Required, StringLength(100)]
        public required string NumeroEmpenho { get; set; }

        public DateTime DataPagamento { get; set; }

        [Required, Column(TypeName = "decimal(18, 2)")]
        public decimal ValorPago { get; set; }

        // Chaves Estrangeiras N:1
        public int OrgaoPublicoId { get; set; }
        public required OrgaoPublico OrgaoPublico { get; set; }

        public int FornecedorId { get; set; }
        public required Fornecedor Fornecedor { get; set; }

        public ICollection<ItemDespesa> Itens { get; set; } = new List<ItemDespesa>();
    }
}