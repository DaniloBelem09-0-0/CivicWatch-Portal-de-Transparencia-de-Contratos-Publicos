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
using System.Globalization;

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

        // =====================================================================
        // 1. CONSULTA DE DADOS PÚBLICOS
        // =====================================================================
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

        // =====================================================================
        // 2. IMPORTAÇÃO DE DADOS (CONTRATOS E DESPESAS)
        // =====================================================================
        public async Task SimulateDataImportAsync()
        {
            var client = _httpClientFactory.CreateClient("PortalTransparenciaClient");
            string logDetail = "";
            int totalContratos = 0;
            int totalDespesas = 0;

            // --- CARREGAR REGRAS ATIVAS ---
            var regrasContratos = await _context.RegrasAlerta
                .Where(r => r.Ativa && r.TipoEntidadeAfetada == "Contrato")
                .ToListAsync();

            var regrasFornecedores = await _context.RegrasAlerta
                .Where(r => r.Ativa && r.TipoEntidadeAfetada == "Fornecedor")
                .ToListAsync();

            var regrasDespesas = await _context.RegrasAlerta
                .Where(r => r.Ativa && r.TipoEntidadeAfetada == "Despesa")
                .ToListAsync();

            // --- ETAPA 1: IMPORTAÇÃO DE CONTRATOS ---
            var orgaoCodes = new List<string> { 
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
                            await PersistirContratos(contratosApi, regrasContratos, regrasFornecedores); 
                            totalContratos += contratosApi.Count;
                        }
                    }
                    else { logDetail += $"ERRO CONTRATOS ({codigo}): {responseContratos.ReasonPhrase}. "; }
                }
                catch (Exception ex) { logDetail += $"ERRO CONTRATOS ({codigo}): {ex.Message}. "; }
                
                await Task.Delay(500); 
            }
            
            // --- ETAPA 2: IMPORTAÇÃO DE DESPESAS ---
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
                        await PersistirDespesas(despesasApi, regrasDespesas);
                        totalDespesas = despesasApi.Count;
                    }
                }
                else { logDetail += $"ERRO DESPESAS: {responseDespesas.ReasonPhrase}. "; }
            }
            catch (Exception ex) { logDetail += $"ERRO DESPESAS: {ex.Message}. "; }

            // Log Final
            _context.LogsAuditoria.Add(new LogAuditoria
            {
                UserId = 1, 
                Acao = "IMPORTACAO_DADOS_PORTAL_EXECUTADA",
                Detalhes = $"Importação finalizada. Contratos: {totalContratos}. Despesas: {totalDespesas}. {logDetail}"
            });
            
            await _context.SaveChangesAsync();
        }
        
        // =====================================================================
        // PERSISTÊNCIA: CONTRATOS
        // =====================================================================
        private async Task PersistirContratos(List<ContratoApiDTO> contratosApi, List<RegraAlerta> regrasAtivas, List<RegraAlerta> regrasFornecedor)
        {
            foreach (var apiContrato in contratosApi)
            {
                if (string.IsNullOrEmpty(apiContrato.Fornecedor?.CnpjFormatado) || string.IsNullOrEmpty(apiContrato.UnidadeGestora?.OrgaoMaximo?.Nome)) continue;

                // 1. Órgão
                var orgaoNome = apiContrato.UnidadeGestora.OrgaoMaximo.Nome;
                var orgao = await _context.OrgaosPublicos.FirstOrDefaultAsync(o => o.Nome == orgaoNome);
                if (orgao == null)
                {
                    orgao = new OrgaoPublico { Nome = orgaoNome, CNPJ = "00000000000000" }; 
                    _context.OrgaosPublicos.Add(orgao);
                    await _context.SaveChangesAsync();
                }

                // 2. Fornecedor
                var cnpjLimpo = apiContrato.Fornecedor.CnpjFormatado.Replace(".", "").Replace("/", "").Replace("-", "");
                var razaoSocial = apiContrato.Fornecedor.RazaoSocialReceita ?? apiContrato.Fornecedor.Nome ?? "Desconhecido";
                var fornecedor = await _context.Fornecedores.FirstOrDefaultAsync(f => f.Documento == cnpjLimpo);

                if (fornecedor == null)
                {
                    string statusSimulado = cnpjLimpo.EndsWith("00") ? "SUSPENSA" : "ATIVA";
                    fornecedor = new Fornecedor { Documento = cnpjLimpo, RazaoSocial = razaoSocial, StatusReceita = statusSimulado };
                    _context.Fornecedores.Add(fornecedor);
                    await _context.SaveChangesAsync();
                    
                    _context.ChecksIntegridade.Add(new CheckIntegridade { FornecedorId = fornecedor.Id, EmConformidade = statusSimulado == "ATIVA" });
                    await _context.SaveChangesAsync();

                    // Trigger de Fornecedor
                    foreach (var regra in regrasFornecedor)
                    {
                        if (!string.IsNullOrEmpty(regra.DescricaoLogica) && regra.DescricaoLogica.Contains("StatusReceita !="))
                        {
                            try {
                                var partes = regra.DescricaoLogica.Split("!=");
                                if (partes.Length == 2) {
                                    var valorEsperado = partes[1].Trim().Replace("'", "").Replace("\"", "");
                                    if (fornecedor.StatusReceita != valorEsperado) {
                                        await _alertaService.CreateAlertaSimplesAsync(regra.Id, $"Fornecedor {fornecedor.RazaoSocial} ({fornecedor.Documento}) possui Status irregular: {fornecedor.StatusReceita}.");
                                    }
                                }
                            } catch { }
                        }
                    }
                }

                // 3. Contrato
                if (!await _context.Contratos.AnyAsync(c => c.NumeroContrato == apiContrato.Numero))
                {
                    var novoContrato = new Contrato
                    {
                        NumeroContrato = apiContrato.Numero ?? $"API-{apiContrato.Id}",
                        ValorTotal = apiContrato.ValorFinalCompra,
                        DataInicio = DateTime.TryParse(apiContrato.DataAssinatura, out var data) ? data : DateTime.Now,
                        OrgaoPublicoId = orgao.Id,
                        OrgaoPublico = orgao, // Previne CS9035
                        FornecedorId = fornecedor.Id,
                        Fornecedor = fornecedor // Previne CS9035
                    };
                    _context.Contratos.Add(novoContrato);

                    // Trigger Dinâmico de Contrato
                    foreach (var regra in regrasAtivas)
                    {
                        if (!string.IsNullOrEmpty(regra.DescricaoLogica) && regra.DescricaoLogica.Contains("ValorTotal >"))
                        {
                            try {
                                var partes = regra.DescricaoLogica.Split('>');
                                if (partes.Length == 2 && decimal.TryParse(partes[1].Trim().Replace("R$", "").Replace(" ", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal valorLimite))
                                {
                                    if (novoContrato.ValorTotal > valorLimite) {
                                        await _alertaService.CreateAlertaSimplesAsync(regra.Id, $"Contrato Nº {novoContrato.NumeroContrato} violou a regra '{regra.Nome}'. Valor: {novoContrato.ValorTotal:C} > Limite: {valorLimite:C}");
                                    }
                                }
                            } catch { }
                        }
                    }
                }
            }
            await _context.SaveChangesAsync();
        }
        
        // =====================================================================
        // PERSISTÊNCIA: DESPESAS
        // =====================================================================
        private async Task PersistirDespesas(List<DespesaApiDTO> despesasApi, List<RegraAlerta> regrasDespesa)
        {
            foreach (var apiDespesa in despesasApi)
            {
                // 1. Órgão
                var orgaoNome = apiDespesa.UnidadeGestora?.Nome ?? "Desconhecido";
                var orgao = await _context.OrgaosPublicos.FirstOrDefaultAsync(o => o.Nome == orgaoNome);
                if (orgao == null)
                {
                    orgao = new OrgaoPublico { Nome = orgaoNome, CNPJ = "00000000000000" };
                    _context.OrgaosPublicos.Add(orgao);
                    await _context.SaveChangesAsync();
                }

                // 2. Fornecedor
                var cnpjLimpo = apiDespesa.FavorecidoCpfCnpj?.Replace(".", "").Replace("/", "").Replace("-", "") ?? "";
                var razaoSocial = apiDespesa.NomeFavorecido ?? "Desconhecido";
                var fornecedor = await _context.Fornecedores.FirstOrDefaultAsync(f => f.Documento == cnpjLimpo);
                if (fornecedor == null)
                {
                    fornecedor = new Fornecedor { Documento = cnpjLimpo, RazaoSocial = razaoSocial, StatusReceita = "ATIVA" };
                    _context.Fornecedores.Add(fornecedor);
                    await _context.SaveChangesAsync();
                    _context.ChecksIntegridade.Add(new CheckIntegridade { FornecedorId = fornecedor.Id, EmConformidade = true });
                    await _context.SaveChangesAsync();
                }

                // 3. Despesa
                if (!await _context.Despesas.AnyAsync(d => d.NumeroEmpenho == apiDespesa.NumeroDocumento))
                {
                    var novaDespesa = new Despesa
                    {
                        NumeroEmpenho = apiDespesa.NumeroDocumento ?? $"API-D-{apiDespesa.Id}",
                        ValorPago = apiDespesa.ValorBruto,
                        DataPagamento = DateTime.TryParse(apiDespesa.DataEmissao, out var data) ? data : DateTime.Now,
                        OrgaoPublicoId = orgao.Id,
                        OrgaoPublico = orgao, // Previne CS9035
                        FornecedorId = fornecedor.Id,
                        Fornecedor = fornecedor // Previne CS9035
                    };
                    _context.Despesas.Add(novaDespesa);
                    _context.ItensDespesa.Add(new ItemDespesa { Despesa = novaDespesa, Descricao = "Item importado", ValorDespesa = novaDespesa.ValorPago });

                    // Trigger Dinâmico de Despesa
                    foreach (var regra in regrasDespesa)
                    {
                        if (!string.IsNullOrEmpty(regra.DescricaoLogica) && regra.DescricaoLogica.Contains("ValorPago >"))
                        {
                            try {
                                var partes = regra.DescricaoLogica.Split('>');
                                if (partes.Length == 2 && decimal.TryParse(partes[1].Trim().Replace("R$", "").Replace(" ", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal valorLimite))
                                {
                                    if (novaDespesa.ValorPago > valorLimite) {
                                        await _alertaService.CreateAlertaSimplesAsync(regra.Id, $"Despesa {novaDespesa.NumeroEmpenho} violou a regra '{regra.Nome}'. Valor: {novaDespesa.ValorPago:C} > Limite: {valorLimite:C}");
                                    }
                                }
                            } catch { }
                        }
                    }
                }
            }
            await _context.SaveChangesAsync();
        }

        // =====================================================================
        // AUDITORIA DE COMPLIANCE (CEIS + INTERNA)
        // =====================================================================
        public async Task CheckSupplierComplianceAsync()
        {
            var client = _httpClientFactory.CreateClient("PortalTransparenciaClient");
            const int regraRiscoId = 2; 

            // 0. Identificar contratos irregulares (Auditoria Interna)
            // Recupera logs de alertas fechados com Justificativa: False
            var logsRejeicao = await _context.LogsAuditoria
                .Where(l => l.Detalhes.Contains("Justificativa: False"))
                .Select(l => l.Detalhes)
                .ToListAsync();

            var alertasRejeitadosIds = new List<int>();
            foreach (var log in logsRejeicao)
            {
                var parts = log.Split(' ');
                // Robustez: tenta extrair ID removendo pontuação
                if (parts.Length > 1 && int.TryParse(parts[1].Trim(',', '.', ':'), out int id)) 
                    alertasRejeitadosIds.Add(id);
            }

            var descricoesAlertas = await _context.Alertas
                .Where(a => alertasRejeitadosIds.Contains(a.Id))
                .Select(a => a.DescricaoOcorrencia)
                .ToListAsync();

            var contratosIrregulares = new HashSet<string>();
            foreach (var desc in descricoesAlertas)
            {
                // Padrão esperado da descrição: "Contrato Nº {numero} ..."
                if (!string.IsNullOrEmpty(desc))
                {
                    var parts = desc.Split(' ');
                    if (parts.Length > 2 && parts[0] == "Contrato" && parts[1] == "Nº")
                        contratosIrregulares.Add(parts[2]);
                }
            }

            // 1. Auditoria em Loop
            var fornecedores = await _context.Fornecedores
                .Include(f => f.CheckIntegridade)
                .Include(f => f.Contratos) // Necessário para auditoria interna
                .ToListAsync();

            int verificados = 0, sancionados = 0;

            foreach (var fornecedor in fornecedores)
            {
                var cnpjLimpo = fornecedor.Documento?.Replace(".", "").Replace("/", "").Replace("-", "") ?? "";
                bool isSanctioned = false;

                // Check A: CEIS (Externo)
                if (cnpjLimpo.Length >= 11)
                {
                    try {
                        var response = await client.GetAsync($"api-de-dados/ceis?cnpjcpfSancionado={cnpjLimpo}&pagina=1");
                        if (response.IsSuccessStatusCode) {
                            var content = await response.Content.ReadAsStringAsync();
                            var sancoes = JsonSerializer.Deserialize<List<CeisSanctionDTO>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            
                            if (sancoes != null && sancoes.Any())
                            {
                                // Filtro inteligente
                                var sancaoEspecifica = sancoes.Any(s => 
                                    (s.CpfCnpjSancionado?.Replace(".", "").Replace("/", "").Replace("-", "") ?? "") == cnpjLimpo
                                );
                                if (sancaoEspecifica) isSanctioned = true;
                            }
                        }
                    } catch (Exception ex) { 
                        _context.LogsAuditoria.Add(new LogAuditoria { UserId = 1, Acao = "ERRO_AUDITORIA_CEIS", Detalhes = ex.Message }); 
                    }
                }

                // Check B: Auditoria Interna (Contratos rejeitados)
                if (!isSanctioned)
                {
                     if (fornecedor.Contratos.Any(c => contratosIrregulares.Contains(c.NumeroContrato)))
                     {
                         isSanctioned = true;
                     }
                }

                if (isSanctioned) sancionados++;

                var check = fornecedor.CheckIntegridade;
                if (check == null) { check = new CheckIntegridade { FornecedorId = fornecedor.Id }; _context.ChecksIntegridade.Add(check); }

                // Trigger de Alerta se mudou para risco
                if (isSanctioned && check.EmConformidade == true)
                {
                     await _alertaService.CreateAlertaSimplesAsync(regraRiscoId, $"Fornecedor {fornecedor.RazaoSocial} ({fornecedor.Documento}) identificado como risco (CEIS ou Auditoria Interna).");
                }

                check.EmConformidade = !isSanctioned;
                check.DataUltimaVerificacao = DateTime.Now;
                verificados++;
            }

            _context.LogsAuditoria.Add(new LogAuditoria
            {
                UserId = 1,
                Acao = "AUDITORIA_CEIS_CONCLUIDA",
                Detalhes = $"Auditoria concluída. {verificados} verificados. {sancionados} riscos identificados."
            });

            await _context.SaveChangesAsync();
        }

        public async Task<List<string>> GetSupplierNonComplianceReasonsAsync(int fornecedorId)
        {
            var reasons = new List<string>();
            var client = _httpClientFactory.CreateClient("PortalTransparenciaClient");
            
            var fornecedor = await _context.Fornecedores
                .Include(f => f.Contratos)
                .Include(f => f.CheckIntegridade)
                .FirstOrDefaultAsync(f => f.Id == fornecedorId);

            if (fornecedor == null) return new List<string> { "Fornecedor não encontrado." };
            if (fornecedor.CheckIntegridade != null && fornecedor.CheckIntegridade.EmConformidade) return new List<string> { "Em conformidade." };

            var cnpjLimpo = fornecedor.Documento?.Replace(".", "").Replace("/", "").Replace("-", "") ?? "";

            // 1. CEIS
            if (cnpjLimpo.Length >= 11)
            {
                try {
                    var response = await client.GetAsync($"api-de-dados/ceis?cnpjcpfSancionado={cnpjLimpo}&pagina=1");
                    if (response.IsSuccessStatusCode) {
                        var content = await response.Content.ReadAsStringAsync();
                        var sancoes = JsonSerializer.Deserialize<List<CeisSanctionDTO>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (sancoes != null && sancoes.Any(s => (s.CpfCnpjSancionado?.Replace(".", "").Replace("/", "").Replace("-", "") ?? "") == cnpjLimpo))
                            reasons.Add("Sanção externa identificada no CEIS.");
                    }
                } catch (Exception ex) { reasons.Add($"Erro API CEIS: {ex.Message}"); }
            }

            // 2. Interna
            var logsRejeicao = await _context.LogsAuditoria.Where(l => l.Detalhes.Contains("Justificativa: False")).Select(l => l.Detalhes).ToListAsync();
            var alertsIds = new List<int>();
            foreach(var l in logsRejeicao) { 
                var p = l.Split(' '); 
                if(p.Length > 1 && int.TryParse(p[1].Trim(',', '.', ':'), out int id)) alertsIds.Add(id); 
            }
            
            var alerts = await _context.Alertas.Where(a => alertsIds.Contains(a.Id)).ToListAsync();
            var contractNums = fornecedor.Contratos.Select(c => c.NumeroContrato).ToHashSet();
            
            foreach(var a in alerts) {
                if(a.DescricaoOcorrencia.StartsWith("Contrato Nº ")) {
                    var num = a.DescricaoOcorrencia.Split(' ')[2];
                    if(contractNums.Contains(num)) reasons.Add($"Auditoria Interna Reprovada no Contrato {num}. Detalhe: {a.DescricaoOcorrencia}");
                }
            }

            if (!reasons.Any()) reasons.Add("Risco identificado, mas detalhes não recuperados (verificar logs manuais).");
            return reasons;
        }
    }
}