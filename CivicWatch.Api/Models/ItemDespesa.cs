using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CivicWatch.Api.Models
{
    public class ItemDespesa
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public required string Descricao { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal ValorDespesa { get; set; }

        // Chave Estrangeira N:1
        public int DespesaId { get; set; }
        public required Despesa Despesa { get; set; }
    }
}