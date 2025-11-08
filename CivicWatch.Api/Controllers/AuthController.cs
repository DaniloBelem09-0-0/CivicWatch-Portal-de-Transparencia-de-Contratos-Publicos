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

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LoginDto login)
        {
            // Substitua 'password' pelo hashing real após implementar o Seeder
            var user = await _authService.AuthenticateAsync(login.Username, "senha_nao_hash"); 

            if (user == null)
            {
                return Unauthorized("Credenciais inválidas.");
            }

            var token = await _authService.GenerateJwtToken(user);
            
            return Ok(new { token = token, role = user.Role.Nome });
        }
    }
}