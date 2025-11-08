using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CivicWatch.Api.Services;
using CivicWatch.Api.DTOs;
using System.Net;

namespace CivicWatch.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FornecedoresController : ControllerBase
    {
        private readonly IFornecedorService _fornecedorService;

        public FornecedoresController(IFornecedorService fornecedorService)
        {
            _fornecedorService = fornecedorService;
        }

        // GET: api/fornecedores (Público)
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<FornecedorResponseDto>>> GetFornecedoresPublico()
        {
            var fornecedores = await _fornecedorService.GetPublicoAsync();
            return Ok(fornecedores);
        }

        // GET: api/fornecedores/{id} (Protegido - Detalhe e Compliance)
        [HttpGet("{id}")]
        [Authorize(Roles = "Auditor, Gestor")]
        public async Task<ActionResult<FornecedorResponseDto>> GetFornecedorDetalhe(int id)
        {
            try
            {
                var fornecedor = await _fornecedorService.GetDetalheAsync(id);
                return Ok(fornecedor);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Fornecedor com ID {id} não encontrado.");
            }
        }

        // POST: api/fornecedores (Protegido - Cadastro)
        [HttpPost]
        [Authorize(Roles = "Auditor, Gestor")]
        public async Task<ActionResult<FornecedorResponseDto>> CadastrarFornecedor([FromBody] FornecedorCadastroDto dto)
        {
            try
            {
                var novoFornecedor = await _fornecedorService.CadastrarFornecedorAsync(dto);
                return CreatedAtAction(nameof(GetFornecedoresPublico), new { id = novoFornecedor.Id }, novoFornecedor);
            }
            catch (Exception ex)
            {
                return BadRequest($"Erro ao cadastrar fornecedor: {ex.Message}");
            }
        }
        
        // PUT: api/fornecedores/{id} (Protegido - Atualização)
        [HttpPut("{id}")]
        [Authorize(Roles = "Auditor, Gestor")]
        public async Task<ActionResult> UpdateFornecedor(int id, [FromBody] FornecedorCadastroDto dto)
        {
            try
            {
                await _fornecedorService.UpdateFornecedorAsync(id, dto);
                return NoContent(); // Status 204: Sucesso, sem conteúdo para retornar
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Fornecedor com ID {id} não encontrado.");
            }
        }

        // DELETE: api/fornecedores/{id} (Protegido - Exclusão de Alto Risco)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Auditor")] // Somente Auditor pode deletar
        public async Task<ActionResult> DeleteFornecedor(int id)
        {
            try
            {
                await _fornecedorService.DeleteFornecedorAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Fornecedor com ID {id} não encontrado.");
            }
        }
    }
}