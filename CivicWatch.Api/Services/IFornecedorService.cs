using CivicWatch.Api.DTOs;

namespace CivicWatch.Api.Services
{
    public interface IFornecedorService
    {
        Task<IEnumerable<FornecedorResponseDto>> GetPublicoAsync();
        Task<FornecedorResponseDto> CadastrarFornecedorAsync(FornecedorCadastroDto dto);
        
        // CORREÇÃO: MÉTODOS QUE FALTAVAM
        Task<FornecedorResponseDto> GetDetalheAsync(int id);
        Task UpdateFornecedorAsync(int id, FornecedorCadastroDto dto);
        Task DeleteFornecedorAsync(int id);
        Task<List<string>> GetSupplierNonComplianceReasonsAsync(int fornecedorId);

    }
}