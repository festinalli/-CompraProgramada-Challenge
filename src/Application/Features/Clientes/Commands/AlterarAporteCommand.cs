using MediatR;
using Application.DTOs;

namespace Application.Features.Clientes.Commands
{
    /// <summary>
    /// Command para alterar valor mensal de aporte.
    /// </summary>
    public class AlterarAporteCommand : IRequest<AdesaoClienteResponse>
    {
        public int ClienteId { get; set; }
        public decimal NovoValorMensalAporte { get; set; }
    }
}
