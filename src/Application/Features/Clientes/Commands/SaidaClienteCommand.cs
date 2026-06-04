using MediatR;
using Application.DTOs;

namespace Application.Features.Clientes.Commands
{
    /// <summary>
    /// Command para saída de cliente.
    /// </summary>
    public class SaidaClienteCommand : IRequest<SaidaClienteResponse>
    {
        public int ClienteId { get; set; }
        public string Motivo { get; set; } = string.Empty;
    }
}
