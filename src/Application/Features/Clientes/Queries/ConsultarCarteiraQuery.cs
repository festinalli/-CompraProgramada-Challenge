using MediatR;
using Application.DTOs;

namespace Application.Features.Clientes.Queries
{
    /// <summary>
    /// Query para consultar carteira do cliente.
    /// Segue padrão CQRS - operação de leitura.
    /// </summary>
    public class ConsultarCarteiraQuery : IRequest<CarteiraClienteResponse>
    {
        public int ClienteId { get; set; }
    }
}
