using Application.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Application.Abstractions
{
    /// <summary>
    /// Abstração de persistência consumida pela camada de aplicação. A inversão
    /// de dependência mantém a Application independente da Infraestrutura (EF/MySQL).
    /// </summary>
    public interface IAppDbContext
    {
        DbSet<Cliente> Clientes { get; }
        DbSet<ContaGrafica> ContasGraficas { get; }
        DbSet<CustodiaFilhote> CustodiasFilhotes { get; }
        DbSet<CustodiaMaster> CustodiasMaster { get; }
        DbSet<CestaRecomendacao> CestasRecomendacao { get; }
        DbSet<ItemCesta> ItensCesta { get; }
        DbSet<HistoricoAporte> HistoricoAportes { get; }
        DbSet<OrdemCompra> OrdensCompra { get; }
        DbSet<DetalheExecucaoOrdem> DetalhesExecucaoOrdem { get; }
        DbSet<EventoIR> EventosIR { get; }
        DbSet<Usuario> Usuarios { get; }
        DbSet<Role> Roles { get; }
        DbSet<Permission> Permissions { get; }
        DbSet<MovimentacaoVenda> MovimentacoesVenda { get; }

        DatabaseFacade Database { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
