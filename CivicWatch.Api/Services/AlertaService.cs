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
        private readonly IFornecedorService _fornecedorService; 

        public AlertaService(ApplicationDbContext context, IFornecedorService fornecedorService)
        {
            _context = context;
            _fornecedorService = fornecedorService;
        }

        // =================================================================
        // MÉTODO DE TRIGGER (ATUALIZADO NA INTERFACE TAMBÉM)
        // =================================================================
        public async Task CreateAlertaSimplesAsync(int regraId, string descricao, int? fornecedorId = null)
        {
            var regra = await _context.RegrasAlerta.FindAsync(regraId);
            var status = await _context.StatusAlertas.FirstOrDefaultAsync(s => s.Nome == "Pendente"); 

            if (regra == null || status == null)
                throw new InvalidOperationException("Falha na configuração: Regra ou Status não encontrado.");

            var newAlerta = new Alerta
            {
                RegraAlertaId = regraId,
                RegraAlerta = regra!, 
                StatusAlertaId = status.Id,
                StatusAlerta = status!,
                DescricaoOcorrencia = descricao,
                DataGeracao = DateTime.UtcNow,
                EntidadeRelacionadaId = fornecedorId,
                TipoEntidadeRelacionada = fornecedorId.HasValue ? "Fornecedor" : null
            };

            _context.Alertas.Add(newAlerta);
            await _context.SaveChangesAsync();
        }

        // =================================================================
        // CRUD REGRAS (Mantido)
        // =================================================================
        public async Task<RegraAlerta> CreateRegraAsync(RegraAlertaDto dto)
        {
            var regra = new RegraAlerta { Nome = dto.Nome, DescricaoLogica = dto.DescricaoLogica, Ativa = dto.Ativa, TipoEntidadeAfetada = dto.TipoEntidadeAfetada };
            _context.RegrasAlerta.Add(regra);
            await _context.SaveChangesAsync();
            return regra;
        }
        
        public async Task<RegraAlertaDto> GetRegraByIdAsync(int id)
        {
            var regra = await _context.RegrasAlerta.Where(r => r.Id == id)
                .Select(r => new RegraAlertaDto { Nome = r.Nome, DescricaoLogica = r.DescricaoLogica, Ativa = r.Ativa, TipoEntidadeAfetada = r.TipoEntidadeAfetada })
                .FirstOrDefaultAsync();
            if (regra == null) throw new KeyNotFoundException($"Regra {id} não encontrada.");
            return regra;
        }

        public async Task UpdateRegraAsync(int id, RegraAlertaDto dto)
        {
            var regra = await _context.RegrasAlerta.FindAsync(id);
            if (regra == null) throw new KeyNotFoundException($"Regra {id} não encontrada.");
            regra.Nome = dto.Nome;
            regra.DescricaoLogica = dto.DescricaoLogica;
            regra.Ativa = dto.Ativa;
            regra.TipoEntidadeAfetada = dto.TipoEntidadeAfetada;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteRegraAsync(int id)
        {
            var regra = await _context.RegrasAlerta.FindAsync(id);
            if (regra == null) throw new KeyNotFoundException($"Regra {id} não encontrada.");
            _context.RegrasAlerta.Remove(regra);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<RegraAlertaDto>> GetRegrasAsync()
        {
             return await _context.RegrasAlerta.Select(r => new RegraAlertaDto { Id = r.Id, Nome = r.Nome, DescricaoLogica = r.DescricaoLogica, Ativa = r.Ativa, TipoEntidadeAfetada = r.TipoEntidadeAfetada }).ToListAsync();
        }

        // =================================================================
        // WORKFLOW DE ALERTA E SINCRONIZAÇÃO (CORRIGIDO)
        // =================================================================

        public async Task<IEnumerable<AlertaResponseDto>> GetAlertasPendentesAsync()
        {
            return await _context.Alertas
                .Where(a => a.StatusAlertaId != 3) 
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
        }

        public async Task SubmitRespostaAsync(int alertaId, RespostaAlertaDto dto, int userId)
        {
            var alerta = await _context.Alertas.FindAsync(alertaId);
            if (alerta == null) throw new KeyNotFoundException("Alerta não encontrado.");

            var resposta = new RespostaAlerta { AlertaId = alertaId, Justificativa = dto.Justificativa, UserId = userId };
            _context.RespostasAlerta.Add(resposta);
            alerta.StatusAlertaId = 2; 
            await _context.SaveChangesAsync();
        }

        public async Task CloseAlertaAsync(int alertaId, int userId, bool justificado, string justificativa)
        {
            var alerta = await _context.Alertas.FindAsync(alertaId);
            if (alerta == null) throw new KeyNotFoundException("Alerta não encontrado.");

            // 1. Atualiza status
            alerta.StatusAlertaId = 3; 

            // 2. Log de Auditoria
            // CORREÇÃO CRÍTICA: Restaurado o padrão "Justificativa: {bool}" para compatibilidade
            // com o FornecedorService, e renomeado o texto livre para "Comentário".
            _context.LogsAuditoria.Add(new LogAuditoria 
            { 
                UserId = userId,
                Acao = $"ALERTA_FECHADO_AUDITOR",
                Detalhes = $"Alerta {alertaId} fechado. Justificativa: {justificado}. Comentário: {justificativa}. Contexto Original: {alerta.DescricaoOcorrencia}"
            });

            await _context.SaveChangesAsync();

            // 3. TRIGGER DE SINCRONIZAÇÃO INTELIGENTE
            int? fornecedorIdParaSync = null;

            // CASO A: Vínculo direto existe
            if (alerta.EntidadeRelacionadaId.HasValue && 
                string.Equals(alerta.TipoEntidadeRelacionada, "Fornecedor", StringComparison.OrdinalIgnoreCase))
            {
                fornecedorIdParaSync = alerta.EntidadeRelacionadaId.Value;
            }
            // CASO B: Fallback (Busca Profunda)
            else if (!string.IsNullOrEmpty(alerta.DescricaoOcorrencia))
            {
                // Estratégia 1: Busca direta por Nome ou Documento do Fornecedor na descrição
                var fornecedorEncontrado = await _context.Fornecedores
                    .Where(f => alerta.DescricaoOcorrencia.Contains(f.RazaoSocial) || 
                               (f.Documento != null && alerta.DescricaoOcorrencia.Contains(f.Documento)))
                    .FirstOrDefaultAsync();

                // Estratégia 2: Se falhar, busca se a descrição cita um CONTRATO do banco
                if (fornecedorEncontrado == null)
                {
                    // OBS: Trazemos todos os contratos para memória para evitar erro de tradução do LINQ (Contains reverso)
                    // Em produção com muitos dados, isso deve ser otimizado com LIKE ou FullTextSearch
                    var todosContratos = await _context.Contratos
                        .Include(c => c.Fornecedor)
                        .ToListAsync();

                    var contratoCitado = todosContratos
                        .FirstOrDefault(c => alerta.DescricaoOcorrencia.Contains(c.NumeroContrato));

                    if (contratoCitado != null && contratoCitado.Fornecedor != null)
                    {
                        fornecedorEncontrado = contratoCitado.Fornecedor;
                    }
                }

                if (fornecedorEncontrado != null)
                {
                    fornecedorIdParaSync = fornecedorEncontrado.Id;
                    
                    // Auto-correção: Salva o vínculo para não precisar buscar da próxima vez
                    alerta.EntidadeRelacionadaId = fornecedorEncontrado.Id;
                    alerta.TipoEntidadeRelacionada = "Fornecedor";
                    await _context.SaveChangesAsync();
                }
            }

            // Se encontrou alguém, força a atualização do status "Em Conformidade"
            if (fornecedorIdParaSync.HasValue)
            {
                await _fornecedorService.SincronizarConformidadeAsync(fornecedorIdParaSync.Value);
            }
        }

        public async Task<RespostaAlertaResponseDto> GetUltimaRespostaAsync(int alertaId)
        {
            var resposta = await _context.RespostasAlerta
                .Where(r => r.AlertaId == alertaId)
                .Include(r => r.User).ThenInclude(u => u.UserProfile)
                .OrderByDescending(r => r.DataResposta)
                .FirstOrDefaultAsync();

            if (resposta == null) throw new KeyNotFoundException("Nenhuma resposta encontrada.");

            return new RespostaAlertaResponseDto
            {
                Justificativa = resposta.Justificativa,
                DataResposta = resposta.DataResposta,
                NomeGestor = resposta.User?.UserProfile?.NomeCompleto ?? "Usuário Desconhecido",
                UsernameGestor = resposta.User?.Username ?? "Desconhecido"
            };
        }
    }
}