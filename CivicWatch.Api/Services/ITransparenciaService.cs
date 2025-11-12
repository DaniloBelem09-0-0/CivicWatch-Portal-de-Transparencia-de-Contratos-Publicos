using CivicWatch.Api.DTOs; 

namespace CivicWatch.Api.Services
{
    public interface ITransparenciaService
    {
        Task<IEnumerable<ContratoResponseDto>> GetContratosAsync();
        // A importação será simulada
        Task SimulateDataImportAsync();
        
        Task CheckSupplierComplianceAsync(); 
    }
}