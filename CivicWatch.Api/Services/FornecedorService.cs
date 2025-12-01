using CivicWatch.Api.Data;
using CivicWatch.Api.Models;
using CivicWatch.Api.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;

namespace CivicWatch.Api.Services
{
    public class FornecedorService : IFornecedorService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory; // NOVO: Necessário para auditoria CEIS

        public FornecedorService(ApplicationDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        // 1. GET PÚBLICO
        public async Task<IEnumerable<FornecedorResponseDto>> GetPublicoAsync()
        {
            return await _context.Fornecedores
                .Include(f => f.CheckIntegridade)
                .Select(f => new FornecedorResponseDto
                {
                    Id = f.Id,
                    Documento = f.Documento,
                    RazaoSocial = f.RazaoSocial,
                    StatusReceita = f.StatusReceita,
                    EmConformidade = f.CheckIntegridade != null && f.CheckIntegridade.EmConformidade
                })
                .ToListAsync();
        }

        // 2. CADASTRO E VALIDAÇÃO
        public async Task<FornecedorResponseDto> CadastrarFornecedorAsync(FornecedorCadastroDto dto)
        {
            var novoFornecedor = new Fornecedor
            {
                Documento = dto.Documento,
                RazaoSocial = dto.RazaoSocial,
            };

            _context.Fornecedores.Add(novoFornecedor);
            await _context.SaveChangesAsync();

            // Simulação de integração inicial
            bool isConforme = !dto.Documento.EndsWith("000"); 

            var check = new CheckIntegridade
            {
                FornecedorId = novoFornecedor.Id,
                DataUltimaVerificacao = DateTime.UtcNow,
                StatusReceitaWs = isConforme ? "ATIVA" : "SUSPENSA",
                EmConformidade = isConforme
            };
            
            _context.ChecksIntegridade.Add(check);
            
            novoFornecedor.StatusReceita = check.StatusReceitaWs;
            await _context.SaveChangesAsync();
            
            return new FornecedorResponseDto
            {
                Id = novoFornecedor.Id,
                Documento = novoFornecedor.Documento,
                RazaoSocial = novoFornecedor.RazaoSocial,
                StatusReceita = check.StatusReceitaWs,
                EmConformidade = check.EmConformidade
            };
        }
        
        // 3. GET DETALHE
        public async Task<FornecedorResponseDto> GetDetalheAsync(int id)
        {
            var fornecedor = await _context.Fornecedores
                .Include(f => f.CheckIntegridade)
                .Where(f => f.Id == id)
                .Select(f => new FornecedorResponseDto
                {
                    Id = f.Id,
                    Documento = f.Documento,
                    RazaoSocial = f.RazaoSocial,
                    StatusReceita = f.StatusReceita,
                    EmConformidade = f.CheckIntegridade != null && f.CheckIntegridade.EmConformidade
                })
                .FirstOrDefaultAsync();

            if (fornecedor == null) throw new KeyNotFoundException($"Fornecedor {id} não encontrado.");
            return fornecedor;
        }

        // 4. UPDATE
        public async Task UpdateFornecedorAsync(int id, FornecedorCadastroDto dto)
        {
            var fornecedor = await _context.Fornecedores.FindAsync(id);
            if (fornecedor == null) throw new KeyNotFoundException();

            fornecedor.RazaoSocial = dto.RazaoSocial;
            // Atualizar outros campos se necessário
            
            await _context.SaveChangesAsync();
        }

        // 5. DELETE
        public async Task DeleteFornecedorAsync(int id)
        {
            var fornecedor = await _context.Fornecedores.FindAsync(id);
            if (fornecedor == null) throw new KeyNotFoundException();

            _context.Fornecedores.Remove(fornecedor);
            await _context.SaveChangesAsync();
        }

        // 6. IMPLEMENTAÇÃO DO MÉTODO DE INCONFORMIDADES (CORREÇÃO DO ERRO CS0535)
        public async Task<List<string>> GetSupplierNonComplianceReasonsAsync(int fornecedorId)
        {
            var reasons = new List<string>();
            var client = _httpClientFactory.CreateClient("PortalTransparenciaClient");
            
            var fornecedor = await _context.Fornecedores
                .Include(f => f.Contratos)
                .Include(f => f.CheckIntegridade) // Importante para ver o status atual
                .FirstOrDefaultAsync(f => f.Id == fornecedorId);

            if (fornecedor == null) return new List<string> { "Fornecedor não encontrado." };

            // Se o fornecedor estiver marcado como "Em Conformidade", retorna imediatamente
            if (fornecedor.CheckIntegridade != null && fornecedor.CheckIntegridade.EmConformidade)
            {
                return new List<string> { "Fornecedor em conformidade." };
            }

            var cnpjLimpo = fornecedor.Documento?.Replace(".", "").Replace("/", "").Replace("-", "") ?? "";

            // --- A. VERIFICAR SANÇÕES CEIS (EXTERNA) ---
            if (cnpjLimpo.Length >= 11)
            {
                var urlCeis = $"api-de-dados/ceis?cnpjcpfSancionado={cnpjLimpo}&pagina=1";
                try
                {
                    var response = await client.GetAsync(urlCeis);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        // Usando o DTO de Sanção que deve estar em CivicWatch.Api.DTOs
                        var sancoes = JsonSerializer.Deserialize<List<CeisSanctionDTO>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        
                        if (sancoes != null && sancoes.Any(s => (s.CpfCnpjSancionado?.Replace(".", "").Replace("/", "").Replace("-", "") ?? "") == cnpjLimpo))
                        {
                            reasons.Add("Sanção ativa identificada no Cadastro Nacional de Empresas Inidôneas e Suspensas (CEIS).");
                        }
                    }
                }
                catch (Exception ex)
                {
                    reasons.Add($"Erro ao consultar API CEIS: {ex.Message}");
                }
            }

            // --- B. VERIFICAR AUDITORIA INTERNA ---
            var supplierContractNumbers = fornecedor.Contratos.Select(c => c.NumeroContrato).ToList();
            
            // Busca logs de rejeição
            var logsRejeicao = await _context.LogsAuditoria
                .Where(l => l.Detalhes.Contains("Justificativa: False"))
                .ToListAsync();

            foreach (var log in logsRejeicao)
            {
                // Verifica se o log menciona algum contrato deste fornecedor
                // Lógica simplificada: procura o numero do contrato no texto do log
                foreach (var contratoNum in supplierContractNumbers)
                {
                    if (log.Detalhes.Contains(contratoNum))
                    {
                        reasons.Add($"Auditoria interna rejeitada para o Contrato Nº {contratoNum}.");
                    }
                }
            }

            if (!reasons.Any())
            {
                reasons.Add("Motivo da inconformidade não detalhado (Status marcado como Risco manualmente ou por simulação).");
            }

            return reasons;
        }
    }
}