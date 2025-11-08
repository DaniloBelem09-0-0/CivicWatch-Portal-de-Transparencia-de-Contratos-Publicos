using CivicWatch.Api.Models;
using CivicWatch.Api.Services; 
using Microsoft.EntityFrameworkCore;

namespace CivicWatch.Api.Data
{
    public static class DataSeeder
    {
        public static async Task SeedRolesAndAdmin(ApplicationDbContext context)
        {
            // Senha padrão para todos os usuários de teste (CRUCIAL PARA TESTES)
            const string defaultPassword = "Teste@123";

            // 1. Inserir Status de Alerta
            if (!context.StatusAlertas.Any())
            {
                context.StatusAlertas.AddRange(
                    new StatusAlerta { Nome = "Pendente", CorHex = "#FFCC00" },
                    new StatusAlerta { Nome = "Em Revisão", CorHex = "#3399FF" },
                    new StatusAlerta { Nome = "Fechado", CorHex = "#33CC33" }
                );
            }

            // 2. Inserir Roles (Papéis de Usuário)
            if (!context.Roles.Any())
            {
                var adminRole = new Role { Nome = "Administrador" };
                var auditorRole = new Role { Nome = "Auditor" };
                var gestorRole = new Role { Nome = "Gestor" };
                var cidadaoRole = new Role { Nome = "Cidadão" };

                context.Roles.AddRange(adminRole, auditorRole, gestorRole, cidadaoRole);
                await context.SaveChangesAsync();

                // 3. Criar os usuários de teste para CADA ROLE
                if (!context.Users.Any())
                {
                    var usersToSeed = new List<User>();

                    // --- ADMIN ---
                    PasswordHasher.CreatePasswordHash(defaultPassword, out byte[] hashAdmin, out byte[] saltAdmin);
                    usersToSeed.Add(new User
                    {
                        Username = "admin_test",
                        PasswordHash = hashAdmin,
                        PasswordSalt = saltAdmin,
                        RoleId = adminRole.Id,
                        Role = adminRole,
                        UserProfile = new UserProfile 
                        { NomeCompleto = "Usuário Admin", Email = "admin@teste.com" }
                    });

                    // --- AUDITOR ---
                    PasswordHasher.CreatePasswordHash(defaultPassword, out byte[] hashAuditor, out byte[] saltAuditor);
                    usersToSeed.Add(new User
                    {
                        Username = "auditor_test",
                        PasswordHash = hashAuditor,
                        PasswordSalt = saltAuditor,
                        RoleId = auditorRole.Id,
                        Role = auditorRole,
                        UserProfile = new UserProfile 
                        { NomeCompleto = "Usuário Auditor", Email = "auditor@teste.com" }
                    });

                    // --- GESTOR ---
                    PasswordHasher.CreatePasswordHash(defaultPassword, out byte[] hashGestor, out byte[] saltGestor);
                    usersToSeed.Add(new User
                    {
                        Username = "gestor_test",
                        PasswordHash = hashGestor,
                        PasswordSalt = saltGestor,
                        RoleId = gestorRole.Id,
                        Role = gestorRole,
                        UserProfile = new UserProfile 
                        { NomeCompleto = "Usuário Gestor", Email = "gestor@teste.com" }
                    });

                    // --- CIDADÃO ---
                    PasswordHasher.CreatePasswordHash(defaultPassword, out byte[] hashCidadao, out byte[] saltCidadao);
                    usersToSeed.Add(new User
                    {
                        Username = "cidadao_test",
                        PasswordHash = hashCidadao,
                        PasswordSalt = saltCidadao,
                        RoleId = cidadaoRole.Id,
                        Role = cidadaoRole,
                        UserProfile = new UserProfile 
                        { NomeCompleto = "Usuário Cidadão", Email = "cidadao@teste.com" }
                    });
                    
                    context.Users.AddRange(usersToSeed);
                }
            }
            await context.SaveChangesAsync();
        }
    }
}