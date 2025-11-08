using CivicWatch.Api.Data;
using CivicWatch.Api.Models;
using CivicWatch.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CivicWatch.Api.Services
{
    public class AlertaService : IAlertaService
    {
        private readonly ApplicationDbContext _context;

        public AlertaService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<RegraAlerta> CreateRegraAsync(RegraAlertaDto dto)
        {
            var regra = new RegraAlerta
            {
                Nome = dto.Nome,
                DescricaoLogica = dto.DescricaoLogica,
                Ativa = dto.Ativa,
                TipoEntidadeAfetada = dto.TipoEntidadeAfetada
            };
            _context.RegrasAlerta.Add(regra);
            await _context.SaveChangesAsync();
            return regra;
        }

        // Simplificado para o MVP: Retorna todos os alertas que não estão Fechados (StatusId != 3)
        public async Task<IEnumerable<AlertaResponseDto>> GetAlertasPendentesAsync()
        {
            var alertas = await _context.Alertas
                .Where(a => a.StatusAlertaId != 3) // Assume que Id 3 é "Fechado"
                .Include(a => a.RegraAlerta)
                .Include(a => a.StatusAlerta)
                .Select(a => new AlertaResponseDto
                {
                    Id = a.Id,
                    DataGeracao = a.DataGeracao,
                    DescricaoOcorrencia = a.DescricaoOcorrencia,
                    RegraNome = a.RegraAlerta.Nome,
                    StatusNome = a.StatusAlerta.Nome,
                    StatusCor = a.StatusAlerta.CorHex 
                })
                .ToListAsync();
            
            return alertas;
        }

        public async Task SubmitRespostaAsync(int alertaId, RespostaAlertaDto dto, int userId)
        {
            var alerta = await _context.Alertas.FindAsync(alertaId);
            if (alerta == null) throw new KeyNotFoundException("Alerta não encontrado.");

            // 1. Cria a resposta
            var resposta = new RespostaAlerta
            {
                AlertaId = alertaId,
                Justificativa = dto.Justificativa,
                UserId = userId
            };
            
            _context.RespostasAlerta.Add(resposta);

            // 2. Atualiza o status para "Em Revisão" (ID 2)
            alerta.StatusAlertaId = 2; 

            await _context.SaveChangesAsync();
        }

        public async Task CloseAlertaAsync(int alertaId, int userId, bool justificado)
        {
            var alerta = await _context.Alertas.FindAsync(alertaId);
            if (alerta == null) throw new KeyNotFoundException("Alerta não encontrado.");

            // 1. Atualiza o status para Fechado (ID 3)
            alerta.StatusAlertaId = 3; 

            // 2. Opcional: Adicionar LogAuditoria
            _context.LogsAuditoria.Add(new LogAuditoria 
            { 
                UserId = userId, // EF Core usa esta FK para criar o log
                Acao = $"ALERTA_FECHADO_AUDITOR",
                Detalhes = $"Alerta {alertaId} fechado. Justificativa: {justificado}"
            });

            await _context.SaveChangesAsync();
        }
    }
}