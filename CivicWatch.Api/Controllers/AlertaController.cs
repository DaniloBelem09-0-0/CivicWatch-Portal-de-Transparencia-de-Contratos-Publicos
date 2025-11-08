using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CivicWatch.Api.Services;
using CivicWatch.Api.DTOs;
using System.Security.Claims; // Para ler a ID do usuário

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

        // GET: api/alerta
        // Permite a consulta por Auditor e Gestor (aqueles que precisam agir)
        [HttpGet]
        [Authorize(Roles = "Auditor, Gestor")]
        public async Task<ActionResult<IEnumerable<AlertaResponseDto>>> GetAlertasPendentes()
        {
            var alertas = await _alertaService.GetAlertasPendentesAsync();
            return Ok(alertas);
        }
        
        // POST: api/alerta/regras
        // Apenas o Auditor pode configurar novas regras de compliance
        [HttpPost("regras")]
        [Authorize(Roles = "Auditor")]
        public async Task<ActionResult> CreateRegra([FromBody] RegraAlertaDto dto)
        {
            var regra = await _alertaService.CreateRegraAsync(dto);
            return CreatedAtAction(nameof(CreateRegra), new { id = regra.Id }, regra);
        }

        // POST: api/alerta/{alertaId}/responder
        // Ação de justificativa, permitida apenas ao Gestor
        [HttpPost("{alertaId}/responder")]
        [Authorize(Roles = "Gestor")]
        public async Task<ActionResult> SubmitResposta(int alertaId, [FromBody] RespostaAlertaDto dto)
        {
            // Obtém a ID do usuário logado (Gestor) a partir do JWT Claim
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
        
        // PUT: api/alerta/{alertaId}/fechar
        // Ação de finalização e aprovação/rejeição, permitida apenas ao Auditor
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
    }
}