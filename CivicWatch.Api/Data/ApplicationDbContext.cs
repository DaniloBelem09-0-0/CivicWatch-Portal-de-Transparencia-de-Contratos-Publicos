using Microsoft.EntityFrameworkCore;
using CivicWatch.Api.Models;

namespace CivicWatch.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // --- 17 DBSETS (A SEREM PREENCHIDOS) ---
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<LogAuditoria> LogsAuditoria { get; set; }
        // ... (Os outros 13 DbSets devem ser preenchidos aqui)

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Relacionamentos 1:1
            modelBuilder.Entity<User>()
                .HasOne<UserProfile>(u => u.UserProfile)
                .WithOne(up => up.User)
                .HasForeignKey<UserProfile>(up => up.UserId)
                .IsRequired();

            modelBuilder.Entity<Fornecedor>()
                .HasOne(f => f.CheckIntegridade)
                .WithOne(ci => ci.Fornecedor)
                .HasForeignKey<CheckIntegridade>(ci => ci.FornecedorId)
                .IsRequired(false); 
            
            // Mapeamento Decimal
            modelBuilder.Entity<Contrato>().Property(c => c.ValorTotal).HasColumnType("decimal(18, 2)");
            // ... (Os outros mapeamentos decimais devem ser incluídos aqui)
        }
    }
}