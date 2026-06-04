using Application.Abstractions;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    public class AppDbContext : DbContext, IAppDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<ContaGrafica> ContasGraficas { get; set; }
        public DbSet<CustodiaFilhote> CustodiasFilhotes { get; set; }
        public DbSet<CustodiaMaster> CustodiasMaster { get; set; }
        public DbSet<CestaRecomendacao> CestasRecomendacao { get; set; }
        public DbSet<ItemCesta> ItensCesta { get; set; }
        public DbSet<HistoricoAporte> HistoricoAportes { get; set; }
        public DbSet<OrdemCompra> OrdensCompra { get; set; }
        public DbSet<DetalheExecucaoOrdem> DetalhesExecucaoOrdem { get; set; }
        public DbSet<EventoIR> EventosIR { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<MovimentacaoVenda> MovimentacoesVenda { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuracoes de precisao decimal
            foreach (var property in modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                property.SetPrecision(18);
                property.SetScale(4);
            }

            // Relacionamentos e indices
            modelBuilder.Entity<Cliente>()
                .HasIndex(c => c.CPF)
                .IsUnique();

            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // RBAC: N:N implícito (Usuario↔Role, Role↔Permission) + nomes únicos.
            modelBuilder.Entity<Role>().HasIndex(r => r.Nome).IsUnique();
            modelBuilder.Entity<Permission>().HasIndex(p => p.Nome).IsUnique();

            modelBuilder.Entity<Cliente>()
                .HasOne(c => c.ContaGrafica)
                .WithOne(cg => cg.Cliente)
                .HasForeignKey<ContaGrafica>(cg => cg.ClienteId);

            modelBuilder.Entity<CestaRecomendacao>()
                .HasMany(c => c.Itens)
                .WithOne(i => i.Cesta)
                .HasForeignKey(i => i.CestaId);

            modelBuilder.Entity<OrdemCompra>()
                .HasMany(o => o.Detalhes)
                .WithOne(d => d.OrdemCompra)
                .HasForeignKey(d => d.OrdemCompraId);
        }
    }
}
