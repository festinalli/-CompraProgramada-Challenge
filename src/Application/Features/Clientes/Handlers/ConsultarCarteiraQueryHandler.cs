using Application.Abstractions;
using MediatR;
using Application.DTOs;
using Application.Features.Clientes.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Clientes.Handlers
{
    /// <summary>
    /// Handler para query de consulta de carteira/rentabilidade.
    /// Operação de leitura - sem efeitos colaterais.
    /// P/L = (cotação atual - preço médio) × quantidade (RN-063..070).
    /// </summary>
    public class ConsultarCarteiraQueryHandler : IRequestHandler<ConsultarCarteiraQuery, CarteiraClienteResponse>
    {
        private readonly IAppDbContext _context;
        private readonly ICotacaoProvider _cotacaoProvider;
        private readonly ILogger<ConsultarCarteiraQueryHandler> _logger;

        public ConsultarCarteiraQueryHandler(
            IAppDbContext context,
            ICotacaoProvider cotacaoProvider,
            ILogger<ConsultarCarteiraQueryHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _cotacaoProvider = cotacaoProvider ?? throw new ArgumentNullException(nameof(cotacaoProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<CarteiraClienteResponse> Handle(ConsultarCarteiraQuery request, CancellationToken cancellationToken)
        {
            var cliente = await _context.Clientes
                .Include(c => c.Custodias)
                .FirstOrDefaultAsync(c => c.Id == request.ClienteId, cancellationToken);

            if (cliente == null)
            {
                throw new InvalidOperationException($"Cliente {request.ClienteId} não encontrado");
            }

            // Consolida por ticker e avalia com a cotação atual (fechamento do último pregão).
            var posicoes = cliente.Custodias
                .GroupBy(cf => cf.Ticker)
                .Select(g =>
                {
                    var quantidade = g.Sum(cf => cf.Quantidade);
                    // Preço médio ponderado pela quantidade entre custódias do mesmo ticker.
                    var custoTotal = g.Sum(cf => cf.Quantidade * cf.PrecoMedio);
                    var precoMedio = quantidade > 0 ? custoTotal / quantidade : 0m;
                    var cotacaoAtual = _cotacaoProvider.ObterPrecoAtual(g.Key) ?? precoMedio;
                    var valorAtual = quantidade * cotacaoAtual;

                    return new PosicaoAtivoResponse
                    {
                        Ticker = g.Key,
                        Quantidade = quantidade,
                        PrecoMedio = precoMedio,
                        CotacaoAtual = cotacaoAtual,
                        ValorAtual = valorAtual,
                        Rentabilidade = valorAtual - custoTotal,
                        PercentualCarteira = 0 // preenchido após o total
                    };
                })
                .ToList();

            var valorTotal = posicoes.Sum(p => p.ValorAtual);
            var valorInvestido = posicoes.Sum(p => p.Quantidade * p.PrecoMedio);
            var rentabilidadeTotal = valorTotal - valorInvestido;

            // Composição percentual real da carteira (RN-070).
            foreach (var p in posicoes)
                p.PercentualCarteira = valorTotal > 0 ? Math.Round(p.ValorAtual / valorTotal * 100, 2) : 0;

            _logger.LogInformation("✅ Carteira consultada. ClienteId: {ClienteId}, ValorTotal: {ValorTotal}",
                request.ClienteId, valorTotal);

            return new CarteiraClienteResponse
            {
                ClienteId = cliente.Id,
                Nome = cliente.Nome,
                SaldoTotal = valorTotal,
                ValorInvestido = valorInvestido,
                ValorAtual = valorTotal,
                Rentabilidade = rentabilidadeTotal,
                PercentualRentabilidade = valorInvestido > 0 ? Math.Round(rentabilidadeTotal / valorInvestido * 100, 2) : 0,
                Posicoes = posicoes
            };
        }
    }
}
