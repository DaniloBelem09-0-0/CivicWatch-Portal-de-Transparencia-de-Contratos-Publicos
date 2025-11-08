using Microsoft.EntityFrameworkCore;
using CivicWatch.Api.Models;
using System;

namespace CivicWatch.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // --- 1. Core do Sistema e Segurança (4 Entidades) ---
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<LogAuditoria> LogsAuditoria { get; set; }

        // --- 2. Transparência e Fornecedores (7 Entidades) ---
        public DbSet<OrgaoPublico> OrgaosPublicos { get; set; }
        public DbSet<Fornecedor> Fornecedores { get; set; }
        public DbSet<Contrato> Contratos { get; set; }
        public DbSet<Despesa> Despesas { get; set; }
        public DbSet<ItemContrato> ItensContrato { get; set; }
        public DbSet<ItemDespesa> ItensDespesa { get; set; }
        public DbSet<FonteDadosPublica> FontesDadosPublica { get; set; }

        // --- 3. Compliance e Workflow de Alertas (6 Entidades) ---
        public DbSet<RegraAlerta> RegrasAlerta { get; set; }
        public DbSet<Alerta> Alertas { get; set; }
        public DbSet<CheckIntegridade> ChecksIntegridade { get; set; }
        public DbSet<RespostaAlerta> RespostasAlerta { get; set; }
        public DbSet<Denuncia> Denuncias { get; set; }
        public DbSet<StatusAlerta> StatusAlertas { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Relacionamento 1:1 (User <-> UserProfile)
            modelBuilder.Entity<User>()
                .HasOne(u => u.UserProfile)
                .WithOne(up => up.User)
                .HasForeignKey<UserProfile>(up => up.UserId)
                .IsRequired();

            // Relacionamento 1:1 Opcional (Fornecedor <-> CheckIntegridade)
            modelBuilder.Entity<Fornecedor>()
                .HasOne(f => f.CheckIntegridade)
                .WithOne(ci => ci.Fornecedor)
                .HasForeignKey<CheckIntegridade>(ci => ci.FornecedorId)
                .IsRequired(false); 
            
            // Mapeamento de Precisão para Moeda
            modelBuilder.Entity<Contrato>().Property(c => c.ValorTotal).HasColumnType("decimal(18, 2)");
            modelBuilder.Entity<Despesa>().Property(d => d.ValorPago).HasColumnType("decimal(18, 2)");
            modelBuilder.Entity<ItemContrato>().Property(ic => ic.ValorUnitario).HasColumnType("decimal(18, 2)");
            modelBuilder.Entity<ItemDespesa>().Property(id => id.ValorDespesa).HasColumnType("decimal(18, 2)");
        }
    }
}