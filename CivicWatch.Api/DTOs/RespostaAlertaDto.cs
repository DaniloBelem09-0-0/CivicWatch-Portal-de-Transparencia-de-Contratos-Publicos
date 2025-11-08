using System.ComponentModel.DataAnnotations;

namespace CivicWatch.Api.DTOs
{
    public class RespostaAlertaDto
    {
        [Required]
        public string Justificativa { get; set; } = string.Empty;
    }
}