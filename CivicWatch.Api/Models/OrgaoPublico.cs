using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace CivicWatch.Api.Models
{
    public class OrgaoPublico
    {
        [Key]
        public int Id { get; set; }
        
        [Required, StringLength(200)]
        public required string Nome { get; set; }
        
        [Required, StringLength(14)]
        public required string CNPJ { get; set; }

        public ICollection<Contrato> Contratos { get; set; } = new List<Contrato>();
        public ICollection<Despesa> Despesas { get; set; } = new List<Despesa>();
    }
}