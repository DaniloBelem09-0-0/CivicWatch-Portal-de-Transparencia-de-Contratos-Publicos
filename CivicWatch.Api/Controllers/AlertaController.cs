using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CivicWatch.Api.Services;
using CivicWatch.Api.DTOs;
using System.Security.Claims; 
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CivicWatch.Api.Controllers
{
    [Authorize] // Todos os endpoints requerem login
    [Route("api/[controller]")]
    [ApiController]
    public class AlertaController : ControllerBase
    {
        private readonly IAlertaService _alertaService;

        public AlertaController(IAlertaService alertaService)
        {
            _alertaService = alertaService;
        }

        // =================================================================
        // ENDPOINTS DE CONSULTA E WORKFLOW DE ALERTA
        // =================================================================

        // GET: api/alerta (Listar Alertas Pendentes)
        [HttpGet]
        [Authorize(Roles = "Auditor, Gestor")]
        public async Task<ActionResult<IEnumerable<AlertaResponseDto>>> GetAlertasPendentes()
        {
            var alertas = await _alertaService.GetAlertasPendentesAsync();
            return Ok(alertas);
        }
        
        // POST: api/alerta/{alertaId}/responder (Ação do Gestor)
        [HttpPost("{alertaId}/responder")]
        [Authorize(Roles = "Gestor")]
        public async Task<ActionResult> SubmitResposta(int alertaId, [FromBody] RespostaAlertaDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized("ID do usuário não encontrada no token.");
            int userId = int.Parse(userIdClaim);

            try
            {
                await _alertaService.SubmitRespostaAsync(alertaId, dto, userId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
        
        // PUT: api/alerta/{alertaId}/fechar (Ação do Auditor)
        [HttpPut("{alertaId}/fechar")]
        [Authorize(Roles = "Auditor")]
        public async Task<ActionResult> CloseAlerta(int alertaId, [FromQuery] bool justificado)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized("ID do usuário não encontrada no token.");
            int userId = int.Parse(userIdClaim);

            try
            {
                await _alertaService.CloseAlertaAsync(alertaId, userId, justificado);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
        
        // =================================================================
        // ENDPOINTS DE GESTÃO DE REGRAS (CRUD RegraAlerta)
        // =================================================================
        
        // GET: api/alerta/regras (Listar Todas as Regras)
        [HttpGet("regras")]
        [Authorize(Roles = "Auditor, Gestor")]
        public async Task<ActionResult<IEnumerable<RegraAlertaDto>>> GetRegras()
        {
            var regras = await _alertaService.GetRegrasAsync();
            return Ok(regras);
        }

        // GET: api/alerta/regras/{id} (Consultar Detalhe da Regra)
        [HttpGet("regras/{id}")]
        [Authorize(Roles = "Auditor")]
        public async Task<ActionResult<RegraAlertaDto>> GetRegraDetalhe(int id)
        {
            try
            {
                var regra = await _alertaService.GetRegraByIdAsync(id);
                return Ok(regra);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
        
        // POST: api/alerta/regras (Criar Regra)
        [HttpPost("regras")]
        [Authorize(Roles = "Auditor")]
        public async Task<ActionResult> CreateRegra([FromBody] RegraAlertaDto dto)
        {
            var regra = await _alertaService.CreateRegraAsync(dto);
            // Redireciona para o GET Detalhe (melhor prática que nameof(CreateRegra))
            return CreatedAtAction(nameof(GetRegraDetalhe), new { id = regra.Id }, regra); 
        }

        // PUT: api/alerta/regras/{id} (Atualizar Regra)
        [HttpPut("regras/{id}")]
        [Authorize(Roles = "Auditor")]
        public async Task<ActionResult> UpdateRegra(int id, [FromBody] RegraAlertaDto dto)
        {
            try
            {
                await _alertaService.UpdateRegraAsync(id, dto);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        // DELETE: api/alerta/regras/{id} (Excluir Regra)
        [HttpDelete("regras/{id}")]
        [Authorize(Roles = "Auditor")]
        public async Task<ActionResult> DeleteRegra(int id)
        {
            try
            {
                await _alertaService.DeleteRegraAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}