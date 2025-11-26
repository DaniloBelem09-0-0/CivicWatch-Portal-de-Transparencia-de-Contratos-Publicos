using CivicWatch.Api.Data;
using CivicWatch.Api.Models;
using CivicWatch.Api.DTOs; // NOVO: Para o DTO de Registro
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CivicWatch.Api.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public AuthService(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // --- Método de Autenticação (Login) ---
        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            var user = await _context.Users
                .Include(u => u.Role) 
                .SingleOrDefaultAsync(u => u.Username == username);

            if (user == null) return null;

            if (user.PasswordHash == null || user.PasswordSalt == null)
            {
                return null; 
            }

            if (!PasswordHasher.VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
            {
                return null;
            }

            return user;
        }
        
        // --- NOVO MÉTODO: Registro de Usuário Comum (Cidadão) ---
        public async Task<User> RegisterAsync(RegisterDto dto)
        {
            // 1. Verificação de Usuário Existente
            if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
            {
                throw new InvalidOperationException("Nome de usuário já existe.");
            }
            
            // 2. Buscar a Role "Cidadão"
            var cidadaoRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Nome == "Cidadão");
            
            if (cidadaoRole == null)
            {
                throw new InvalidOperationException("A Role 'Cidadão' não foi encontrada. Execute o Seeder.");
            }

            // 3. Criar Hash da Senha
            PasswordHasher.CreatePasswordHash(dto.Password, out byte[] hash, out byte[] salt);

            // 4. Criar as Entidades (User e UserProfile)
            var newUser = new User
            {
                Username = dto.Username,
                PasswordHash = hash,
                PasswordSalt = salt,
                RoleId = cidadaoRole.Id,
                Role = cidadaoRole,
                UserProfile = new UserProfile 
                {
                    NomeCompleto = dto.NomeCompleto,
                    Email = dto.Email
                    // Outros campos 'required' do UserProfile devem ser preenchidos aqui
                }
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();
            
            return newUser;
        }

        public async Task<string> GenerateJwtToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role.Nome)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? throw new InvalidOperationException("Chave JWT não configurada.")));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(7),
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}