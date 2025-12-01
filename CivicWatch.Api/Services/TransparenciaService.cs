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
using System.Globalization; // Necessário para formatação numérica robusta

namespace CivicWatch.Api.Services
{
    // NOTA: As classes auxiliares (ContratoApiDTO, etc.) devem estar em CivicWatch.Api.DTOs

    public class TransparenciaService : ITransparenciaService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAlertaService _alertaService;

        public TransparenciaService(ApplicationDbContext context, IHttpClientFactory httpClientFactory, IAlertaService alertaService)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _alertaService = alertaService;
        }

        // 1. Consulta de Contratos (Dados Públicos - Lendo do DB Local)
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
            // PASSO 0: CARREGAR REGRAS ATIVAS DINAMICAMENTE
            // =========================================================
            // Busca regras ativas para "Contrato" para aplicar durante a importação
            var regrasContratosAtivas = await _context.RegrasAlerta
                .Where(r => r.Ativa && r.TipoEntidadeAfetada == "Contrato")
                .ToListAsync();

            // =========================================================
            // ETAPA 1: IMPORTAÇÃO DE CONTRATOS - MÚLTIPLOS ÓRGÃOS
            // =========================================================
            var orgaoCodes = new List<string> 
            { 
                "20000", "25000", "32000", "39000", "53000", "54000", "55000", "81000", "91000", "92000"
            };

            foreach (var codigo in orgaoCodes)
            {
                var urlContratos = $"api-de-dados/contratos?exercicio=2024&pagina=1&codigoOrgao={codigo}"; 
                
                try
                {
                    var responseContratos = await client.GetAsync(urlContratos);
                    
                    if (responseContratos.IsSuccessStatusCode)
                    {
                        var content = await responseContratos.Content.ReadAsStringAsync();
                        var contratosApi = JsonSerializer.Deserialize<List<ContratoApiDTO>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        
                        if (contratosApi != null && contratosApi.Any())
                        {
                            // Passa as regras carregadas para a função de persistência
                            await PersistirContratos(contratosApi, regrasContratosAtivas); 
                            totalContratos += contratosApi.Count;
                        }
                    }
                    else
                    {
                        logDetail += $"ERRO CONTRATOS (Órgão {codigo}, Status {responseContratos.StatusCode}): {responseContratos.ReasonPhrase}. ";
                    }
                }
                catch (Exception ex)
                {
                    logDetail += $"ERRO CONTRATOS (Órgão {codigo}, EX): {ex.Message}. ";
                }
                
                await Task.Delay(500); 
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

            // 3. Registrar o log
            _context.LogsAuditoria.Add(new LogAuditoria
            {
                UserId = 1, 
                Acao = "IMPORTACAO_DADOS_PORTAL_EXECUTADA",
                Detalhes = $"Importação finalizada. Contratos: {totalContratos}. Despesas: {totalDespesas}. {logDetail}"
            });
            
            await _context.SaveChangesAsync();
        }
        
        // =====================================================================
        // FUNÇÃO DE PERSISTÊNCIA: CONTRATOS (COM AVALIAÇÃO DINÂMICA DE REGRAS)
        // =====================================================================
        
        private async Task PersistirContratos(List<ContratoApiDTO> contratosApi, List<RegraAlerta> regrasAtivas)
        {
            foreach (var apiContrato in contratosApi)
            {
                if (string.IsNullOrEmpty(apiContrato.Fornecedor?.CnpjFormatado) || string.IsNullOrEmpty(apiContrato.UnidadeGestora?.OrgaoMaximo?.Nome)) continue;

                // 1. Busca/Cria Órgão
                var orgaoNome = apiContrato.UnidadeGestora.OrgaoMaximo.Nome;
                var orgao = await _context.OrgaosPublicos.FirstOrDefaultAsync(o => o.Nome == orgaoNome);
                if (orgao == null)
                {
                    orgao = new OrgaoPublico { Nome = orgaoNome, CNPJ = "00000000000000" }; 
                    _context.OrgaosPublicos.Add(orgao);
                    await _context.SaveChangesAsync();
                }

                // 2. Busca/Cria Fornecedor
                var cnpjLimpo = apiContrato.Fornecedor.CnpjFormatado.Replace(".", "").Replace("/", "").Replace("-", "");
                var razaoSocial = apiContrato.Fornecedor.RazaoSocialReceita ?? apiContrato.Fornecedor.Nome ?? "Fornecedor Desconhecido";
                var fornecedor = await _context.Fornecedores.FirstOrDefaultAsync(f => f.Documento == cnpjLimpo);

                if (fornecedor == null)
                {
                    fornecedor = new Fornecedor { Documento = cnpjLimpo, RazaoSocial = razaoSocial, StatusReceita = "ATIVA" };
                    _context.Fornecedores.Add(fornecedor);
                    await _context.SaveChangesAsync();
                    _context.ChecksIntegridade.Add(new CheckIntegridade { FornecedorId = fornecedor.Id, EmConformidade = true });
                    await _context.SaveChangesAsync();
                }

                // 3. CRIA O CONTRATO
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

                    // =========================================================
                    // APLICAÇÃO DAS REGRAS DINÂMICAS
                    // =========================================================
                    foreach (var regra in regrasAtivas)
                    {
                        if (!string.IsNullOrEmpty(regra.DescricaoLogica) && regra.DescricaoLogica.Contains("ValorTotal >"))
                        {
                            try 
                            {
                                var partes = regra.DescricaoLogica.Split('>');
                                if (partes.Length == 2)
                                {
                                    var valorString = partes[1].Trim().Replace("R$", "").Replace(" ", "");
                                    
                                    if (decimal.TryParse(valorString, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal valorLimite))
                                    {
                                        if (novoContrato.ValorTotal > valorLimite)
                                        {
                                            await _alertaService.CreateAlertaSimplesAsync(
                                                regra.Id, 
                                                $"Contrato Nº {novoContrato.NumeroContrato} violou a regra '{regra.Nome}'. Valor: {novoContrato.ValorTotal:C} > Limite: {valorLimite:C}"
                                            );
                                        }
                                    }
                                }
                            }
                            catch
                            {
                                Console.WriteLine($"Erro ao processar regra: {regra.Nome}");
                            }
                        }
                    }
                }
            }
            await _context.SaveChangesAsync();
        }
        
        private async Task PersistirDespesas(List<DespesaApiDTO> despesasApi)
        {
            foreach (var apiDespesa in despesasApi)
            {
                var orgaoNome = apiDespesa.UnidadeGestora?.Nome ?? "Órgão Desconhecido";
                var orgao = await _context.OrgaosPublicos.FirstOrDefaultAsync(o => o.Nome == orgaoNome);
                if (orgao == null) { orgao = new OrgaoPublico { Nome = orgaoNome, CNPJ = "00" }; _context.OrgaosPublicos.Add(orgao); await _context.SaveChangesAsync(); }

                var cnpjLimpo = apiDespesa.FavorecidoCpfCnpj?.Replace(".", "").Replace("/", "").Replace("-", "") ?? "";
                var fornecedor = await _context.Fornecedores.FirstOrDefaultAsync(f => f.Documento == cnpjLimpo);
                if (fornecedor == null) { fornecedor = new Fornecedor { Documento = cnpjLimpo, RazaoSocial = apiDespesa.NomeFavorecido ?? "Desconhecido", StatusReceita = "ATIVA" }; _context.Fornecedores.Add(fornecedor); await _context.SaveChangesAsync(); }

                if (!await _context.Despesas.AnyAsync(d => d.NumeroEmpenho == apiDespesa.NumeroDocumento))
                {
                    var novaDespesa = new Despesa { NumeroEmpenho = apiDespesa.NumeroDocumento ?? "API", ValorPago = apiDespesa.ValorBruto, DataPagamento = DateTime.Now, OrgaoPublicoId = orgao.Id, FornecedorId = fornecedor.Id, OrgaoPublico = orgao, Fornecedor = fornecedor };
                    _context.Despesas.Add(novaDespesa);
                    _context.ItensDespesa.Add(new ItemDespesa { Despesa = novaDespesa, Descricao = "Item importado", ValorDespesa = novaDespesa.ValorPago });
                }
            }
            await _context.SaveChangesAsync();
        }

        // =====================================================================
        // AUDITORIA DE COMPLIANCE: CEIS + AUDITORIA INTERNA (ALERTAS REJEITADOS)
        // =====================================================================
        public async Task CheckSupplierComplianceAsync()
        {
            var client = _httpClientFactory.CreateClient("PortalTransparenciaClient");
            const int regraRiscoId = 2; 

            // 0. PASSO PRÉVIO: Identificar contratos irregulares (Alerta fechado e NÃO justificado)
            var logsRejeicao = await _context.LogsAuditoria
                .Where(l => l.Detalhes.Contains("Justificativa: False"))
                .Select(l => l.Detalhes)
                .ToListAsync();

            var alertasRejeitadosIds = new List<int>();
            foreach (var log in logsRejeicao)
            {
                var parts = log.Split(' ');
                if (parts.Length > 1 && int.TryParse(parts[1], out int id))
                {
                    alertasRejeitadosIds.Add(id);
                }
            }

            var descricoesAlertasRejeitados = await _context.Alertas
                .Where(a => alertasRejeitadosIds.Contains(a.Id))
                .Select(a => a.DescricaoOcorrencia)
                .ToListAsync();

            var contratosIrregulares = new HashSet<string>();
            foreach (var desc in descricoesAlertasRejeitados)
            {
                if (!string.IsNullOrEmpty(desc))
                {
                    var parts = desc.Split(' ');
                    // Padrão: "Contrato Nº {numero} ..."
                    if (parts.Length > 2 && parts[0] == "Contrato" && parts[1] == "Nº")
                    {
                        contratosIrregulares.Add(parts[2]);
                    }
                }
            }

            // 1. Obter fornecedores e seus contratos
            var fornecedores = await _context.Fornecedores
                .Include(f => f.CheckIntegridade)
                .Include(f => f.Contratos)
                .ToListAsync();

            int fornecedoresVerificados = 0;
            int fornecedoresSancionados = 0;

            foreach (var fornecedor in fornecedores)
            {
                var cnpjLimpo = fornecedor.Documento?.Replace(".", "").Replace("/", "").Replace("-", "") ?? "";
                bool isSanctioned = false;
                
                // --- VERIFICAÇÃO 1: Auditoria Externa (CEIS) ---
                if (cnpjLimpo.Length >= 11)
                {
                    var urlCeis = $"api-de-dados/ceis?cnpjcpfSancionado={cnpjLimpo}&pagina=1";
                    try
                    {
                        var response = await client.GetAsync(urlCeis);
                        if (response.IsSuccessStatusCode)
                        {
                            var content = await response.Content.ReadAsStringAsync();
                            var sancoes = JsonSerializer.Deserialize<List<CeisSanctionDTO>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            
                            if (sancoes != null && sancoes.Any())
                            {
                                var sancaoEspecifica = sancoes.Any(s => 
                                {
                                    var sancaoCnpjLimpo = s.CpfCnpjSancionado?.Replace(".", "").Replace("/", "").Replace("-", "") ?? "";
                                    return sancaoCnpjLimpo == cnpjLimpo;
                                });
                                
                                if (sancaoEspecifica) isSanctioned = true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _context.LogsAuditoria.Add(new LogAuditoria { UserId = 1, Acao = "ERRO_AUDITORIA_CEIS", Detalhes = ex.Message });
                    }
                }

                // --- VERIFICAÇÃO 2: Auditoria Interna (Contratos Rejeitados) ---
                if (!isSanctioned && fornecedor.Contratos.Any(c => contratosIrregulares.Contains(c.NumeroContrato)))
                {
                    isSanctioned = true;
                }

                if (isSanctioned) fornecedoresSancionados++;

                // Atualização do CheckIntegridade
                var checkIntegridade = fornecedor.CheckIntegridade;
                if (checkIntegridade == null)
                {
                    checkIntegridade = new CheckIntegridade { FornecedorId = fornecedor.Id };
                    _context.ChecksIntegridade.Add(checkIntegridade);
                }

                // GATILHO DE ALERTA DE RISCO (Se mudou de OK para RISCO)
                if (isSanctioned && checkIntegridade.EmConformidade == true)
                {
                     await _alertaService.CreateAlertaSimplesAsync(
                        regraRiscoId, 
                        $"Fornecedor {fornecedor.RazaoSocial} (CNPJ: {fornecedor.Documento}) identificado como risco (CEIS ou Auditoria Interna)."
                    );
                }

                checkIntegridade.EmConformidade = !isSanctioned;
                checkIntegridade.DataUltimaVerificacao = DateTime.Now;
                fornecedoresVerificados++;
            }

            _context.LogsAuditoria.Add(new LogAuditoria
            {
                UserId = 1,
                Acao = "AUDITORIA_CEIS_CONCLUIDA",
                Detalhes = $"Auditoria CEIS concluída. {fornecedoresVerificados} verificados. {fornecedoresSancionados} identificados com risco."
            });

            await _context.SaveChangesAsync();
        }

        // =====================================================================
        // NOVO MÉTODO: OBTENÇÃO DE DETALHES DAS INCONFORMIDADES
        // =====================================================================
        
        public async Task<List<string>> GetSupplierNonComplianceReasonsAsync(int fornecedorId)
        {
            var reasons = new List<string>();
            var client = _httpClientFactory.CreateClient("PortalTransparenciaClient");
            
            var fornecedor = await _context.Fornecedores
                .Include(f => f.Contratos)
                .FirstOrDefaultAsync(f => f.Id == fornecedorId);

            if (fornecedor == null)
            {
                reasons.Add("Fornecedor não encontrado.");
                return reasons;
            }

            var cnpjLimpo = fornecedor.Documento?.Replace(".", "").Replace("/", "").Replace("-", "") ?? "";

            // --- 1. VERIFICAR SANÇÕES CEIS (EXTERNA) ---
            if (cnpjLimpo.Length >= 11)
            {
                var urlCeis = $"api-de-dados/ceis?cnpjcpfSancionado={cnpjLimpo}&pagina=1";
                try
                {
                    var response = await client.GetAsync(urlCeis);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var sancoes = JsonSerializer.Deserialize<List<CeisSanctionDTO>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        
                        // Verifica se há sanção específica para o CNPJ
                        if (sancoes != null && sancoes.Any(s => (s.CpfCnpjSancionado?.Replace(".", "").Replace("/", "").Replace("-", "") ?? "") == cnpjLimpo))
                        {
                            reasons.Add("Sanção externa identificada no Cadastro Nacional de Empresas Inidôneas e Suspensas (CEIS).");
                            // Adicionar detalhes se necessário
                        }
                    }
                }
                catch (Exception ex)
                {
                    reasons.Add($"Erro ao consultar API CEIS: {ex.Message}");
                }
            }

            // --- 2. VERIFICAR AUDITORIA INTERNA (ALERTA NÃO JUSTIFICADO) ---
            
            var supplierContractNumbers = fornecedor.Contratos.Select(c => c.NumeroContrato).ToList();
            
            // Busca logs de rejeição ("Justificativa: False")
            var logsRejeicao = await _context.LogsAuditoria
                .Where(l => l.Detalhes.Contains("Justificativa: False"))
                .Select(l => l.Detalhes)
                .ToListAsync();

            var alertasRejeitadosIds = new List<int>();
            foreach (var log in logsRejeicao)
            {
                var parts = log.Split(' ');
                if (parts.Length > 1 && int.TryParse(parts[1], out int id))
                {
                    alertasRejeitadosIds.Add(id);
                }
            }

            // Busca os alertas que foram rejeitados
            var nonCompliantAlerts = await _context.Alertas
                .Where(a => alertasRejeitadosIds.Contains(a.Id))
                .ToListAsync();

            foreach (var alerta in nonCompliantAlerts)
            {
                // Padrão: "Contrato Nº {numero}..."
                if (alerta.DescricaoOcorrencia.StartsWith("Contrato Nº "))
                {
                    var contractNumber = alerta.DescricaoOcorrencia.Split(' ')[2];
                    
                    // Se o contrato irregular pertence a este fornecedor
                    if (supplierContractNumbers.Contains(contractNumber))
                    {
                        reasons.Add($"Alerta interno no Contrato Nº {contractNumber} foi marcado como 'Não Justificado' pelo Auditor. Detalhe: {alerta.DescricaoOcorrencia}");
                    }
                }
            }

            if (!reasons.Any())
            {
                reasons.Add("Nenhuma inconformidade registrada ou identificada na última auditoria.");
            }

            return reasons;
        }
    }
}