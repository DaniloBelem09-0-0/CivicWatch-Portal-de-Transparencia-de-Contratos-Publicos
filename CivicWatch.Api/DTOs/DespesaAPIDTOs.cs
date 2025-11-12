namespace CivicWatch.Api.DTOs
{
    public class DespesaApiDTO
    {
        public long Id { get; set; }
        public string? NumeroDocumento { get; set; }
        public decimal ValorBruto { get; set; } 
        public string? DataEmissao { get; set; } 
        // Você precisaria de DTOs aninhados para Orgao, Favorecido (Fornecedor), etc.
        public UnidadeGestoraDTO? UnidadeGestora { get; set; }
        public string? FavorecidoCpfCnpj { get; set; }
        public string? NomeFavorecido { get; set; }
    }
}