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
    // NOTA: As classes auxiliares (ContratoApiDTO, DespesaApiDTO, CeisSanctionDTO, etc.)
    // Devem estar definidas no namespace CivicWatch.Api.DTOs para que este arquivo compile.

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
            string logDetail = "";
            int totalContratos = 0;
            int totalDespesas = 0;

            // =========================================================
            // ETAPA 1: IMPORTAÇÃO DE CONTRATOS
            // =========================================================
            var urlContratos = "api-de-dados/contratos?exercicio=2024&pagina=1&codigoOrgao=26000"; 
            
            try
            {
                var responseContratos = await client.GetAsync(urlContratos);
                
                if (responseContratos.IsSuccessStatusCode)
                {
                    var content = await responseContratos.Content.ReadAsStringAsync();
                    var contratosApi = JsonSerializer.Deserialize<List<ContratoApiDTO>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (contratosApi != null && contratosApi.Any())
                    {
                        await PersistirContratos(contratosApi); 
                        totalContratos = contratosApi.Count;
                    }
                }
                else
                {
                    logDetail += $"ERRO CONTRATOS (Status {responseContratos.StatusCode}): {responseContratos.ReasonPhrase}. ";
                }
            }
            catch (Exception ex)
            {
                logDetail += $"ERRO CONTRATOS (EX): {ex.Message}. ";
            }
            
            // =========================================================
            // ETAPA 2: IMPORTAÇÃO DE DESPESAS
            // =========================================================
            var dataFiltro = DateTime.Now.ToString("dd/MM/yyyy");
            var urlDespesas = $"api-de-dados/despesas/documentos?dataEmissao={dataFiltro}&fase=3&pagina=1&unidadeGestora=170001"; 
            
            try
            {
                var responseDespesas = await client.GetAsync(urlDespesas);

                if (responseDespesas.IsSuccessStatusCode)
                {
                    var content = await responseDespesas.Content.ReadAsStringAsync();
                    var despesasApi = JsonSerializer.Deserialize<List<DespesaApiDTO>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (despesasApi != null && despesasApi.Any())
                    {
                        await PersistirDespesas(despesasApi);
                        totalDespesas = despesasApi.Count;
                    }
                }
                else
                {
                    logDetail += $"ERRO DESPESAS (Status {responseDespesas.StatusCode}): {responseDespesas.ReasonPhrase}. ";
                }
            }
            catch (Exception ex)
            {
                logDetail += $"ERRO DESPESAS (EX): {ex.Message}. ";
            }

            // 3. Registrar o log (Ação crítica de auditoria)
            _context.LogsAuditoria.Add(new LogAuditoria
            {
                UserId = 1, 
                Acao = "IMPORTACAO_DADOS_PORTAL_EXECUTADA",
                Detalhes = $"Importação finalizada. Contratos Processados: {totalContratos}. Despesas Processadas: {totalDespesas}. Erros: {logDetail}"
            });
            
            await _context.SaveChangesAsync();
        }
        
        // =====================================================================
        // FUNÇÃO DE PERSISTÊNCIA: CONTRATOS
        // =====================================================================
        
        private async Task PersistirContratos(List<ContratoApiDTO> contratosApi)
        {
            foreach (var apiContrato in contratosApi)
            {
                // Verifica dados críticos
                if (string.IsNullOrEmpty(apiContrato.Fornecedor?.CnpjFormatado) || string.IsNullOrEmpty(apiContrato.UnidadeGestora?.OrgaoMaximo?.Nome)) continue;

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
        // FUNÇÃO DE PERSISTÊNCIA: DESPESAS
        // =====================================================================
        
        private async Task PersistirDespesas(List<DespesaApiDTO> despesasApi)
        {
            foreach (var apiDespesa in despesasApi)
            {
                // 1. GARANTIR O ÓRGÃO PÚBLICO (Simplificado)
                var orgaoNome = apiDespesa.UnidadeGestora?.Nome ?? "Órgão Desconhecido";
                var orgao = await _context.OrgaosPublicos.FirstOrDefaultAsync(o => o.Nome == orgaoNome);

                if (orgao == null)
                {
                    orgao = new OrgaoPublico { Nome = orgaoNome, CNPJ = "00000000000000" };
                    _context.OrgaosPublicos.Add(orgao);
                    await _context.SaveChangesAsync();
                }

                // 2. GARANTIR O FORNECEDOR (Favorecido)
                var cnpjLimpo = apiDespesa.FavorecidoCpfCnpj?.Replace(".", "").Replace("/", "").Replace("-", "") ?? "";
                var razaoSocial = apiDespesa.NomeFavorecido ?? "Favorecido Desconhecido";

                var fornecedor = await _context.Fornecedores.FirstOrDefaultAsync(f => f.Documento == cnpjLimpo);

                if (fornecedor == null)
                {
                    fornecedor = new Fornecedor { Documento = cnpjLimpo, RazaoSocial = razaoSocial, StatusReceita = "ATIVA" };
                    _context.Fornecedores.Add(fornecedor);
                    await _context.SaveChangesAsync();
                }

                // 3. CRIAR A DESPESA
                if (!await _context.Despesas.AnyAsync(d => d.NumeroEmpenho == apiDespesa.NumeroDocumento))
                {
                    var novaDespesa = new Despesa
                    {
                        NumeroEmpenho = apiDespesa.NumeroDocumento ?? $"API-D-{apiDespesa.Id}",
                        ValorPago = apiDespesa.ValorBruto,
                        DataPagamento = DateTime.TryParse(apiDespesa.DataEmissao, out var data) ? data : DateTime.Now,
                        OrgaoPublicoId = orgao.Id,
                        OrgaoPublico = orgao,
                        FornecedorId = fornecedor.Id,
                        Fornecedor = fornecedor
                    };
                    _context.Despesas.Add(novaDespesa);
                    
                    // Simular ItemDespesa
                    _context.ItensDespesa.Add(new ItemDespesa { Despesa = novaDespesa, Descricao = "Item de despesa importado", ValorDespesa = novaDespesa.ValorPago });
                }
            }
            await _context.SaveChangesAsync();
        }

        // NOVO MÉTODO: Auditoria de Compliance (Sanções CEIS)
        public async Task CheckSupplierComplianceAsync()
        {
            var client = _httpClientFactory.CreateClient("PortalTransparenciaClient");
            
            // 1. Obter todos os fornecedores do DB local com seu CheckIntegridade
            var fornecedores = await _context.Fornecedores
                .Include(f => f.CheckIntegridade)
                .ToListAsync();

            int fornecedoresVerificados = 0;
            int fornecedoresSancionados = 0;

            foreach (var fornecedor in fornecedores)
            {
                var cnpjLimpo = fornecedor.Documento?.Replace(".", "").Replace("/", "").Replace("-", "") ?? "";
                if (cnpjLimpo.Length < 11) continue; // Pula documentos inválidos

                // 2. Preparar a chamada para a API do CEIS
                var urlCeis = $"api-de-dados/ceis?cnpjcpfSancionado={cnpjLimpo}&pagina=1";
                bool isSanctioned = false;

                try
                {
                    var response = await client.GetAsync(urlCeis);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        // Deserializa para a lista de sanções (se a lista não for vazia, o fornecedor está sancionado)
                        var sancoes = JsonSerializer.Deserialize<List<CeisSanctionDTO>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        
                        if (sancoes != null && sancoes.Any())
                        {
                            isSanctioned = true;
                            fornecedoresSancionados++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Registra erro de conexão/API, mas continua o loop
                    _context.LogsAuditoria.Add(new LogAuditoria 
                    { 
                        UserId = 1, 
                        Acao = "ERRO_AUDITORIA_CEIS", 
                        Detalhes = $"Falha na consulta CEIS para {cnpjLimpo}: {ex.Message}" 
                    });
                }

                // 3. Atualizar o CheckIntegridade
                var checkIntegridade = fornecedor.CheckIntegridade;
                if (checkIntegridade == null)
                {
                    // Cria o registro se ele não existir (embora deva ser criado na importação)
                    checkIntegridade = new CheckIntegridade { FornecedorId = fornecedor.Id };
                    _context.ChecksIntegridade.Add(checkIntegridade);
                }

                // Se o status de risco mudar para não-conforme, podemos gerar um alerta
                if (isSanctioned && checkIntegridade.EmConformidade == true)
                {
                    // Lógica para gerar um ALERTA de risco (se você tiver um AlertaService)
                    // Ex: await _alertaService.CreateAlertaAsync(fornecedor.Id, "RISCO_CEIS", "Fornecedor encontrado no cadastro de inidôneos.");
                }

                checkIntegridade.EmConformidade = !isSanctioned;
                checkIntegridade.DataUltimaVerificacao = DateTime.Now;
                fornecedoresVerificados++;
            }

            // Registro Final
            _context.LogsAuditoria.Add(new LogAuditoria
            {
                UserId = 1,
                Acao = "AUDITORIA_CEIS_CONCLUIDA",
                Detalhes = $"Auditoria CEIS concluída. {fornecedoresVerificados} fornecedores verificados. {fornecedoresSancionados} sancionados."
            });

            await _context.SaveChangesAsync();
        }
    }
}
