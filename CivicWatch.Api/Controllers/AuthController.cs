using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CivicWatch.Api.DTOs;
using CivicWatch.Api.Services;

namespace CivicWatch.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // Endpoint para Login (Retorna JWT)
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LoginDto login)
        {
            var user = await _authService.AuthenticateAsync(login.Username, login.Password); 

            if (user == null)
            {
                return Unauthorized("Credenciais inválidas.");
            }

            var token = await _authService.GenerateJwtToken(user);
            
            return Ok(new { token = token, role = user.Role.Nome, username = user.Username });
        }

        // NOVO: Endpoint para Registro de Usuário Comum (Cidadão)
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                var user = await _authService.RegisterAsync(dto);
                
                // Autentica o usuário para retornar o token após o registro
                var token = await _authService.GenerateJwtToken(user);

                // Retorna 201 Created com o token para o Front-End
                return CreatedAtAction(nameof(Register), new 
                { 
                    token = token, 
                    role = user.Role.Nome,
                    username = user.Username 
                });
            }
            catch (InvalidOperationException ex)
            {
                // Tratamento para "Usuário já existe" ou "Role não existe"
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                // Erro interno (hashing, banco, etc.)
                return StatusCode(500, $"Erro interno ao registrar usuário: {ex.Message}");
            }
        }
    }
}