using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CivicWatch.Api.Services;
using CivicWatch.Api.DTOs;

namespace CivicWatch.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransparenciaController : ControllerBase
    {
        private readonly ITransparenciaService _transparenciaService;

        public TransparenciaController(ITransparenciaService transparenciaService)
        {
            _transparenciaService = transparenciaService;
        }

        [HttpGet("contratos")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ContratoResponseDto>>> GetContratos()
        {
            var contratos = await _transparenciaService.GetContratosAsync();
            return Ok(contratos);
        }

        [HttpPost("importar")]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult> ImportarDados()
        {
            await _transparenciaService.SimulateDataImportAsync();
            return Ok("Simulação de importação iniciada e logada com sucesso.");
        }
        
        [HttpPost("auditar")]
        [Authorize(Roles = "Auditor, Administrador")]
        public async Task<ActionResult> AuditarCompliance()
        {
            await _transparenciaService.CheckSupplierComplianceAsync();
            
            return Ok("Auditoria de Compliance (CEIS/CNEP) disparada com sucesso. Verifique o Log de Auditoria para resultados.");
        }
    }
}