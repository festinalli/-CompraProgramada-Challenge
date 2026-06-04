using Application.Abstractions;
using MediatR;
using Application.DTOs;
using Application.Features.Admin.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Admin.Handlers
{
    /// <summary>
    /// Handler para consulta da cesta Top Five.
    /// Operação de leitura - sem efeitos colaterais.
    /// </summary>
    public class ConsultarCestaQueryHandler : IRequestHandler<ConsultarCestaQuery, CestaResponse>
    {
        private readonly IAppDbContext _context;
        private readonly ILogger<ConsultarCestaQueryHandler> _logger;

        public ConsultarCestaQueryHandler(IAppDbContext context, ILogger<ConsultarCestaQueryHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<CestaResponse> Handle(ConsultarCestaQuery request, CancellationToken cancellationToken)
        {
            var cesta = await _context.CestasRecomendacao
                .Include(c => c.Itens)
                .Where(c => c.Ativa)
                .FirstOrDefaultAsync(cancellationToken);

            if (cesta == null)
            {
                _logger.LogWarning("⚠️ Nenhuma cesta ativa encontrada");
                throw new InvalidOperationException("Nenhuma cesta ativa cadastrada");
            }

            var ativos = cesta.Itens
                .Select(a => new CestaAtivoResponse
                {
                    Ticker = a.Ticker,
                    Percentual = a.Percentual
                })
                .ToList();

            _logger.LogInformation("✅ Cesta consultada. ID: {CestaId}, Ativos: {Quantidade}",
                cesta.Id, ativos.Count);

            return new CestaResponse
            {
                CestaId = cesta.Id,
                DataCadastro = cesta.DataCriacao,
                DataCriacao = cesta.DataCriacao,
                Ativa = cesta.Ativa,
                Ativos = ativos,
                Mensagem = "Cesta consultada com sucesso"
            };
        }
    }
}
