using Application.Abstractions;
using MediatR;
using Application.DTOs;
using Application.Features.Clientes.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Clientes.Handlers
{
    /// <summary>
    /// Handler para comando de saída de cliente.
    /// O cliente fica inativo mas MANTÉM a posição (sem liquidar). O SaldoFinal
    /// retornado é o valor de mercado atual da carteira mantida (informativo).
    /// </summary>
    public class SaidaClienteCommandHandler : IRequestHandler<SaidaClienteCommand, SaidaClienteResponse>
    {
        private readonly IAppDbContext _context;
        private readonly ICotacaoProvider _cotacaoProvider;
        private readonly ILogger<SaidaClienteCommandHandler> _logger;

        public SaidaClienteCommandHandler(
            IAppDbContext context,
            ICotacaoProvider cotacaoProvider,
            ILogger<SaidaClienteCommandHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _cotacaoProvider = cotacaoProvider ?? throw new ArgumentNullException(nameof(cotacaoProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<SaidaClienteResponse> Handle(SaidaClienteCommand request, CancellationToken cancellationToken)
        {
            var cliente = await _context.Clientes
                .Include(c => c.Custodias)
                .FirstOrDefaultAsync(c => c.Id == request.ClienteId, cancellationToken);

            if (cliente == null)
            {
                throw new InvalidOperationException($"Cliente {request.ClienteId} não encontrado");
            }

            cliente.Ativo = false;
            cliente.DataSaida = DateTime.UtcNow;
            cliente.MotivoSaida = request.Motivo;

            await _context.SaveChangesAsync(cancellationToken);

            var saldoFinal = cliente.Custodias.Sum(cf =>
                cf.Quantidade * (_cotacaoProvider.ObterPrecoAtual(cf.Ticker) ?? cf.PrecoMedio));

            _logger.LogInformation("✅ Cliente saiu (posição mantida). ClienteId: {ClienteId}", cliente.Id);

            return new SaidaClienteResponse
            {
                ClienteId = cliente.Id,
                Nome = cliente.Nome,
                SaldoFinal = saldoFinal,
                DataSaida = cliente.DataSaida ?? DateTime.UtcNow,
                Mensagem = "Saída processada com sucesso (posição mantida)"
            };
        }
    }
}
