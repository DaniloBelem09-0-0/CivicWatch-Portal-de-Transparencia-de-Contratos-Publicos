using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic;

namespace CivicWatch.Api.Models
{
    public class Contrato
    {
        [Key]
        public int Id { get; set; }
        
        [Required, StringLength(100)]
        public required string NumeroContrato { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal ValorTotal { get; set; }

        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }

        // Chaves Estrangeiras N:1
        public int OrgaoPublicoId { get; set; }
        public required OrgaoPublico OrgaoPublico { get; set; }

        public int FornecedorId { get; set; }
        public required Fornecedor Fornecedor { get; set; }

        public ICollection<ItemContrato> Itens { get; set; } = new List<ItemContrato>();
    }
}