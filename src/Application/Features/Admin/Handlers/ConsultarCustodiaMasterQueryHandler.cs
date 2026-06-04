using Application.Abstractions;
using MediatR;
using Application.DTOs;
using Application.Features.Admin.Queries;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Admin.Handlers
{
    public class ConsultarCustodiaMasterQueryHandler
        : IRequestHandler<ConsultarCustodiaMasterQuery, List<CustodiaMasterResponse>>
    {
        private readonly IAppDbContext _context;

        public ConsultarCustodiaMasterQueryHandler(IAppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<List<CustodiaMasterResponse>> Handle(
            ConsultarCustodiaMasterQuery request, CancellationToken cancellationToken)
        {
            var custodias = await _context.CustodiasMaster
                .Where(m => m.Quantidade > 0)
                .OrderBy(m => m.Ticker)
                .ToListAsync(cancellationToken);

            return custodias.Select(m => new CustodiaMasterResponse
            {
                Ticker = m.Ticker,
                Quantidade = m.Quantidade,
                QuantidadeLotePadrao = m.QuantidadeLotePadrao,
                QuantidadeFracionario = m.QuantidadeFracionario,
                PrecoMedio = m.PrecoMedio,
                ValorAtual = m.ValorAtual
            }).ToList();
        }
    }
}
