using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace CivicWatch.Api.DTOs
{
    // DTO principal que representa o objeto completo do Contrato retornado pela API
    public class ContratoApiDTO
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }
        
        [JsonPropertyName("numero")]
        public string? Numero { get; set; }
        
        [JsonPropertyName("objeto")]
        public string? Objeto { get; set; }
        
        [JsonPropertyName("dataAssinatura")]
        public string? DataAssinatura { get; set; } // Será convertida para DateTime

        // Valor final da compra é a melhor representação do Valor Total
        [JsonPropertyName("valorFinalCompra")]
        public decimal ValorFinalCompra { get; set; } 
        
        [JsonPropertyName("unidadeGestora")]
        public UnidadeGestoraDTO? UnidadeGestora { get; set; }
        
        [JsonPropertyName("fornecedor")]
        public FornecedorApiDTO? Fornecedor { get; set; }
    }

    // Estrutura para a Unidade Gestora (o OrgaoPublico no seu sistema)
    public class UnidadeGestoraDTO
    {
        [JsonPropertyName("codigo")]
        public string? Codigo { get; set; }
        
        [JsonPropertyName("nome")]
        public string? Nome { get; set; }
        
        [JsonPropertyName("orgaoMaximo")]
        public OrgaoMaximoDTO? OrgaoMaximo { get; set; }
    }

    // Estrutura para o Órgão Máximo
    public class OrgaoMaximoDTO
    {
        [JsonPropertyName("sigla")]
        public string? Sigla { get; set; }
        
        [JsonPropertyName("nome")]
        public string? Nome { get; set; }
    }

    // Estrutura para o Fornecedor
    public class FornecedorApiDTO
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }
        
        [JsonPropertyName("cnpjFormatado")]
        public string? CnpjFormatado { get; set; } // Usar este para o Documento
        
        [JsonPropertyName("nome")]
        public string? Nome { get; set; } // Nome do fornecedor
        
        [JsonPropertyName("razaoSocialReceita")]
        public string? RazaoSocialReceita { get; set; } // Razão Social Completa
    }
}