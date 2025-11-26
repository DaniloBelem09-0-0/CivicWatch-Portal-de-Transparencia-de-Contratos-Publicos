using CivicWatch.Api.DTOs; 

namespace CivicWatch.Api.Services
{
    public interface ITransparenciaService
    {
        Task<IEnumerable<ContratoResponseDto>> GetContratosAsync();
        Task SimulateDataImportAsync();
        
        Task CheckSupplierComplianceAsync(); 
    }
}