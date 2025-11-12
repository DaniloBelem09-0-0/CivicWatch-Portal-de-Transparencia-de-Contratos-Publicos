// DTOs para mapear a resposta do Portal da Transparência (CEIS)
namespace CivicWatch.Api.DTOs
{
    public class CeisSanctionDTO
    {
        // A API do CEIS retorna uma lista de objetos se o CNPJ/CPF estiver sancionado.
        // Basta mapear as propriedades principais para verificar se a lista não está vazia.
        public string? NomeSancionado { get; set; }
        public string? Sancao { get; set; }
        public string? DataFinalSancao { get; set; }
        public string? CpfCnpjSancionado { get; set; } 
        // Outras propriedades podem ser mapeadas conforme a necessidade de registro.
    }
}