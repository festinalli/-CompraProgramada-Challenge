using Application.Abstractions;
using MediatR;
using Domain.Constants;
using Application.DTOs;
using Application.Features.Clientes.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Application.Features.Clientes.Handlers
{
    /// <summary>
    /// Handler para comando de alteração de aporte.
    /// </summary>
    public class AlterarAporteCommandHandler : IRequestHandler<AlterarAporteCommand, AdesaoClienteResponse>
    {
        private readonly IAppDbContext _context;
        private readonly ILogger<AlterarAporteCommandHandler> _logger;

        public AlterarAporteCommandHandler(IAppDbContext context, ILogger<AlterarAporteCommandHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<AdesaoClienteResponse> Handle(AlterarAporteCommand request, CancellationToken cancellationToken)
        {
            if (request.NovoValorMensalAporte < ConstantesNegocio.VALOR_MINIMO_APORTE)
            {
                throw new InvalidOperationException(
                    $"Valor mínimo de aporte é R$ {ConstantesNegocio.VALOR_MINIMO_APORTE}");
            }

            var cliente = await _context.Clientes.FindAsync(new object[] { request.ClienteId }, cancellationToken);
            if (cliente == null)
            {
                throw new InvalidOperationException($"Cliente {request.ClienteId} não encontrado");
            }

            cliente.ValorMensalAporte = request.NovoValorMensalAporte;
            _context.Clientes.Update(cliente);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("✅ Aporte alterado. ClienteId: {ClienteId}, NovoValor: {NovoValor}", 
                cliente.Id, request.NovoValorMensalAporte);

            return new AdesaoClienteResponse
            {
                ClienteId = cliente.Id,
                Nome = cliente.Nome,
                CPFMascarado = MascararCPF(cliente.CPF),
                ValorMensalAporte = cliente.ValorMensalAporte,
                DataAdesao = cliente.DataAdesao,
                Mensagem = "Aporte alterado com sucesso"
            };
        }

        private string MascararCPF(string cpf)
        {
            cpf = Regex.Replace(cpf, @"\D", "");
            if (cpf.Length != 11) return "***.***.***-**";
            return $"{cpf.Substring(0, 3)}.{cpf.Substring(3, 3)}.{cpf.Substring(6, 3)}-**";
        }
    }
}
