using CivicWatch.Api.Data;
using CivicWatch.Api.Models;
using CivicWatch.Api.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace CivicWatch.Api.Services
{
    public class AlertaService : IAlertaService
    {
        private readonly ApplicationDbContext _context;

        public AlertaService(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================================
        // MÉTODO SIMPLES PARA DISPARAR ALERTAS POR TRIGGER (R$ 1M / CEIS)
        // =========================================================================
        public async Task CreateAlertaSimplesAsync(int regraId, string descricao)
        {
            // 1. Busca a Regra e o Status
            var regra = await _context.RegrasAlerta.FindAsync(regraId);
            var status = await _context.StatusAlertas.FirstOrDefaultAsync(s => s.Nome == "Pendente"); 

            // Validação de segurança
            if (regra == null || status == null)
            {
                throw new InvalidOperationException("Falha na configuração: Regra ou Status de Alerta 'Pendente' não encontrado no banco.");
            }

            var newAlerta = new Alerta
            {
                RegraAlertaId = regraId,
                
                // CORREÇÃO CS8601: Usamos o operador '!' porque já validamos que não são nulos no 'if' acima.
                RegraAlerta = regra!, 
                
                StatusAlertaId = status.Id,
                StatusAlerta = status!, // O '!' silencia o aviso CS8601
                
                DescricaoOcorrencia = descricao,
                DataGeracao = DateTime.UtcNow
            };

            _context.Alertas.Add(newAlerta);
            await _context.SaveChangesAsync();
        }


        // =========================================================================
        // MÉTODOS DE CRIAÇÃO E CONSULTA DE REGRAS (CRUD Regras)
        // =========================================================================

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
        
        public async Task<RegraAlertaDto> GetRegraByIdAsync(int id)
        {
            var regra = await _context.RegrasAlerta
                .Where(r => r.Id == id)
                .Select(r => new RegraAlertaDto 
                {
                    Nome = r.Nome,
                    DescricaoLogica = r.DescricaoLogica,
                    Ativa = r.Ativa,
                    TipoEntidadeAfetada = r.TipoEntidadeAfetada
                })
                .FirstOrDefaultAsync();

            if (regra == null)
            {
                throw new KeyNotFoundException($"Regra de Alerta com ID {id} não encontrada.");
            }
            return regra;
        }

        public async Task UpdateRegraAsync(int id, RegraAlertaDto dto)
        {
            var regra = await _context.RegrasAlerta.FindAsync(id);
            if (regra == null)
            {
                throw new KeyNotFoundException($"Regra de Alerta com ID {id} não encontrada.");
            }

            regra.Nome = dto.Nome;
            regra.DescricaoLogica = dto.DescricaoLogica;
            regra.Ativa = dto.Ativa;
            regra.TipoEntidadeAfetada = dto.TipoEntidadeAfetada;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteRegraAsync(int id)
        {
            var regra = await _context.RegrasAlerta.FindAsync(id);
            if (regra == null)
            {
                throw new KeyNotFoundException($"Regra de Alerta com ID {id} não encontrada.");
            }

            _context.RegrasAlerta.Remove(regra);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<RegraAlertaDto>> GetRegrasAsync()
        {
             return await _context.RegrasAlerta
                .Select(r => new RegraAlertaDto 
                {
                    Id = r.Id,
                    Nome = r.Nome,
                    DescricaoLogica = r.DescricaoLogica,
                    Ativa = r.Ativa,
                    TipoEntidadeAfetada = r.TipoEntidadeAfetada
                })
                .ToListAsync();
        }

        // =========================================================================
        // MÉTODOS DO WORKFLOW DE ALERTA
        // =========================================================================

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

            // 2. Adicionar LogAuditoria
            _context.LogsAuditoria.Add(new LogAuditoria 
            { 
                UserId = userId,
                Acao = $"ALERTA_FECHADO_AUDITOR",
                Detalhes = $"Alerta {alertaId} fechado. Justificativa: {justificado}"
            });

            await _context.SaveChangesAsync();
        }
    }
}