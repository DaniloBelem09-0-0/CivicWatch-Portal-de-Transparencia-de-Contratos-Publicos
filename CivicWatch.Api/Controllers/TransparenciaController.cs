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

        // GET: api/transparencia/contratos (PÚBLICO)
        [HttpGet("contratos")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ContratoResponseDto>>> GetContratos()
        {
            var contratos = await _transparenciaService.GetContratosAsync();
            return Ok(contratos);
        }

        // POST: api/transparencia/importar (PROTEGIDO)
        [HttpPost("importar")]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult> ImportarDados()
        {
            await _transparenciaService.SimulateDataImportAsync();
            return Ok("Simulação de importação iniciada e logada com sucesso.");
        }
        
        // FALTANTE: Adicionar um GET para despesas também.
    }
}