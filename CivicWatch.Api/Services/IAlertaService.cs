using CivicWatch.Api.Models;
using CivicWatch.Api.DTOs;

namespace CivicWatch.Api.Services
{
    public interface IAlertaService
    {
        // CRUD de RegraAlerta (Auditor)
        Task<RegraAlerta> CreateRegraAsync(RegraAlertaDto dto);

        // Consulta de Alertas (Auditor/Gestor)
        Task<IEnumerable<AlertaResponseDto>> GetAlertasPendentesAsync();

        // Submissão de Resposta (Gestor)
        Task SubmitRespostaAsync(int alertaId, RespostaAlertaDto dto, int userId);

        // Finalização do Alerta (Auditor)
        Task CloseAlertaAsync(int alertaId, int userId, bool justificado);
    }
}