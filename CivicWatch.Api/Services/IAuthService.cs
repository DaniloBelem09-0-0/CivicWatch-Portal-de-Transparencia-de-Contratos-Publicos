using CivicWatch.Api.Models;

namespace CivicWatch.Api.Services
{
    public interface IAuthService
    {
        Task<User?> AuthenticateAsync(string username, string password);
        Task<string> GenerateJwtToken(User user);
    }
}