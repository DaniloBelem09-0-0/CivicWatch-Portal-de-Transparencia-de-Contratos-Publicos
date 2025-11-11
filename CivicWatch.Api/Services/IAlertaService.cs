using CivicWatch.Api.Models;
using CivicWatch.Api.DTOs;

namespace CivicWatch.Api.Services
{
    public interface IAlertaService
    {
        // CRUD de RegraAlerta (Auditor)
        Task<RegraAlerta> CreateRegraAsync(RegraAlertaDto dto);
        
        // NOVO: Listar Todas as Regras (Corrigindo o CS1061)
        Task<IEnumerable<RegraAlertaDto>> GetRegrasAsync(); 

        // Consulta de Alertas (Auditor/Gestor)
        Task<IEnumerable<AlertaResponseDto>> GetAlertasPendentesAsync();

        // Submissão de Resposta (Gestor)
        Task SubmitRespostaAsync(int alertaId, RespostaAlertaDto dto, int userId);

        // Finalização do Alerta (Auditor)
        Task CloseAlertaAsync(int alertaId, int userId, bool justificado);

        // CRUD de Regras
        Task<RegraAlertaDto> GetRegraByIdAsync(int id); 
        Task UpdateRegraAsync(int id, RegraAlertaDto dto);
        Task DeleteRegraAsync(int id);
    }
}