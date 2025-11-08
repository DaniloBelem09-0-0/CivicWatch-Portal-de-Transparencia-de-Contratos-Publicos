using CivicWatch.Api.DTOs; // Necessário criar este DTO
using CivicWatch.Api.Models;

namespace CivicWatch.Api.Services
{
    public interface IAuthService
    {
        Task<User?> AuthenticateAsync(string username, string password);
        Task<string> GenerateJwtToken(User user);
        
        // NOVO: Método para registro de usuário comum (Cidadão)
        Task<User> RegisterAsync(RegisterDto dto); 
    }
}