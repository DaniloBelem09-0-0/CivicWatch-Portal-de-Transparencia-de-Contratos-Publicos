using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CivicWatch.Api.Models
{
    public class ItemContrato
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public required string Descricao { get; set; }

        [Required]
        public int Quantidade { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal ValorUnitario { get; set; }

        // Chave Estrangeira N:1
        public int ContratoId { get; set; }
        public required Contrato Contrato { get; set; }
    }
}