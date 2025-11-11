using CivicWatch.Api.Data;
using CivicWatch.Api.Models;
using CivicWatch.Api.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace CivicWatch.Api.Services
{
    public class TransparenciaService : ITransparenciaService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public TransparenciaService(ApplicationDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        // 1. Consulta de Contratos (Dados Públicos - Lendo do DB)
        public async Task<IEnumerable<ContratoResponseDto>> GetContratosAsync()
        {
            return await _context.Contratos
                .Include(c => c.OrgaoPublico)
                .Include(c => c.Fornecedor)
                .Select(c => new ContratoResponseDto
                {
                    Id = c.Id,
                    NumeroContrato = c.NumeroContrato,
                    ValorTotal = c.ValorTotal,
                    DataInicio = c.DataInicio,
                    OrgaoNome = c.OrgaoPublico.Nome,
                    FornecedorRazaoSocial = c.Fornecedor.RazaoSocial
                })
                .ToListAsync();
        }

        // 2. Importação de Dados do Portal da Transparência (Consumo e Mapeamento)
        public async Task SimulateDataImportAsync()
        {
            var client = _httpClientFactory.CreateClient("PortalTransparenciaClient");
            
            // URL CORRETA
            var url = "api-de-dados/contratos?exercicio=2024&pagina=1&codigoOrgao=26000"; 
            string logDetail;

            try
            {
                var response = await client.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    
                    // DESSERIALIZAÇÃO DO JSON
                    var contratosApi = JsonSerializer.Deserialize<List<ContratoApiDTO>>(
                        content, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    // CHAMADA CORRIGIDA: PERSISTÊNCIA REAL
                    if (contratosApi != null && contratosApi.Any())
                    {
                        await PersistirContratos(contratosApi); // <-- Salva no DB
                        logDetail = $"Importação BEM-SUCEDIDA. Status HTTP: {response.StatusCode}. {contratosApi.Count} contratos persistidos.";
                    } else {
                         logDetail = $"Importação BEM-SUCEDIDA, mas a API retornou dados vazios.";
                    }
                }
                else
                {
                    logDetail = $"Importação FALHOU. Status HTTP: {response.StatusCode}. Motivo: {response.ReasonPhrase}.";
                }
            }
            catch (Exception ex)
            {
                 logDetail = $"ERRO CRÍTICO DE CONEXÃO/MAPEAMENTO: {ex.Message}.";
            }

            // 3. Registrar o log (Ação crítica de auditoria)
            _context.LogsAuditoria.Add(new LogAuditoria
            {
                UserId = 1, // Assumindo Admin
                Acao = "IMPORTACAO_DADOS_PORTAL_EXECUTADA",
                Detalhes = logDetail
            });
            
            await _context.SaveChangesAsync();
        }
        
        // =====================================================================
        // FUNÇÃO DE PERSISTÊNCIA REAL (NOVA FUNÇÃO)
        // =====================================================================
        
        private async Task PersistirContratos(List<ContratoApiDTO> contratosApi)
        {
            foreach (var apiContrato in contratosApi)
            {
                // Verifica dados críticos antes de tentar mapear
                if (string.IsNullOrEmpty(apiContrato.Fornecedor?.CnpjFormatado) || apiContrato.UnidadeGestora?.OrgaoMaximo?.Nome == null) continue;

                // 1. GARANTIR O ÓRGÃO PÚBLICO (OrgaoMaximo)
                var orgaoNome = apiContrato.UnidadeGestora.OrgaoMaximo.Nome;
                var orgao = await _context.OrgaosPublicos.FirstOrDefaultAsync(o => o.Nome == orgaoNome);

                if (orgao == null)
                {
                    orgao = new OrgaoPublico { Nome = orgaoNome, CNPJ = "00000000000000" }; // CNPJ mock
                    _context.OrgaosPublicos.Add(orgao);
                    await _context.SaveChangesAsync();
                }

                // 2. GARANTIR O FORNECEDOR
                var cnpjLimpo = apiContrato.Fornecedor.CnpjFormatado.Replace(".", "").Replace("/", "").Replace("-", "");
                var razaoSocial = apiContrato.Fornecedor.RazaoSocialReceita ?? apiContrato.Fornecedor.Nome ?? "Fornecedor Desconhecido";
                
                var fornecedor = await _context.Fornecedores.FirstOrDefaultAsync(f => f.Documento == cnpjLimpo);

                if (fornecedor == null)
                {
                    fornecedor = new Fornecedor { Documento = cnpjLimpo, RazaoSocial = razaoSocial, StatusReceita = "ATIVA" };
                    _context.Fornecedores.Add(fornecedor);
                    await _context.SaveChangesAsync();
                    
                    // Cria o CheckIntegridade mock para o novo fornecedor
                    _context.ChecksIntegridade.Add(new CheckIntegridade { FornecedorId = fornecedor.Id, EmConformidade = true });
                    await _context.SaveChangesAsync();
                }

                // 3. CRIAR O CONTRATO
                if (!await _context.Contratos.AnyAsync(c => c.NumeroContrato == apiContrato.Numero))
                {
                    var novoContrato = new Contrato
                    {
                        NumeroContrato = apiContrato.Numero ?? $"API-{apiContrato.Id}",
                        ValorTotal = apiContrato.ValorFinalCompra,
                        DataInicio = DateTime.TryParse(apiContrato.DataAssinatura, out var data) ? data : DateTime.Now,
                        OrgaoPublicoId = orgao.Id,
                        FornecedorId = fornecedor.Id
                    };
                    _context.Contratos.Add(novoContrato);
                }
            }
            await _context.SaveChangesAsync();
        }

        // =====================================================================
        // FUNÇÃO MAPEAMENTO ANTIGA (AGORA NÃO USADA)
        // =====================================================================
        private List<Contrato> MapearContratos(List<ContratoApiDTO> apiContratos)
        {
             // Esta função não é mais usada, pois a lógica de persistência e mapeamento está em PersistirContratos
             return new List<Contrato>();
        }
    }
}