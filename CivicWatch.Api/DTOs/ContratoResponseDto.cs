using System.ComponentModel.DataAnnotations.Schema;

namespace CivicWatch.Api.DTOs
{
    public class ContratoResponseDto
    {
        public int Id { get; set; }
        public string NumeroContrato { get; set; } = string.Empty;
        public decimal ValorTotal { get; set; }
        public DateTime DataInicio { get; set; }
        public string OrgaoNome { get; set; } = string.Empty;
        public string FornecedorRazaoSocial { get; set; } = string.Empty;
    }
}