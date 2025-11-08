using CivicWatch.Api.Models;

namespace CivicWatch.Api.DTOs
{
    // Usado para retornar alertas com informações essenciais da Regra e do Status
    public class AlertaResponseDto
    {
        public int Id { get; set; }
        public DateTime DataGeracao { get; set; }
        public string? DescricaoOcorrencia { get; set; }
        
        // Dados da Regra
        public string RegraNome { get; set; } = string.Empty;
        
        // Dados do Status
        public string StatusNome { get; set; } = string.Empty;
        public string StatusCor { get; set; } = string.Empty;
    }
}