using Application.Abstractions;
using MediatR;
using Application.DTOs;
using Application.Features.Admin.Queries;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Admin.Handlers
{
    public class ConsultarHistoricoCestasQueryHandler
        : IRequestHandler<ConsultarHistoricoCestasQuery, List<CestaHistoricoResponse>>
    {
        private readonly IAppDbContext _context;

        public ConsultarHistoricoCestasQueryHandler(IAppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<List<CestaHistoricoResponse>> Handle(
            ConsultarHistoricoCestasQuery request, CancellationToken cancellationToken)
        {
            var cestas = await _context.CestasRecomendacao
                .Include(c => c.Itens)
                .OrderByDescending(c => c.DataCriacao)
                .ToListAsync(cancellationToken);

            return cestas.Select(c => new CestaHistoricoResponse
            {
                CestaId = c.Id,
                Nome = c.Nome,
                Ativa = c.Ativa,
                DataCriacao = c.DataCriacao,
                DataDesativacao = c.DataDesativacao,
                Ativos = c.Itens.Select(i => new CestaAtivoResponse
                {
                    Ticker = i.Ticker,
                    Percentual = i.Percentual
                }).ToList()
            }).ToList();
        }
    }
}
