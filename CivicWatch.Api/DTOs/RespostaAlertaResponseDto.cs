using System;

namespace CivicWatch.Api.DTOs
{
    public class RespostaAlertaResponseDto
    {
        public string Justificativa { get; set; } = string.Empty;
        public DateTime DataResposta { get; set; }
        public string NomeGestor { get; set; } = string.Empty;
        public string UsernameGestor { get; set; } = string.Empty;
    }
}