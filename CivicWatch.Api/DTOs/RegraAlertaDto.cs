using System.ComponentModel.DataAnnotations;

namespace CivicWatch.Api.DTOs
{
    public class RegraAlertaDto
    {
        public int Id {get; set;} = 0;

        [Required]
        public string Nome { get; set; } = string.Empty;
        
        [Required]
        public string DescricaoLogica { get; set; } = string.Empty; // Ex: "valorContrato > 100000"
        
        public bool Ativa { get; set; } = true;

        // Opcional no DTO, o Service pode inferir ou deixar em branco
        public string? TipoEntidadeAfetada { get; set; }
    }
}