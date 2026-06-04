using MediatR;
using Application.DTOs;

namespace Application.Features.Admin.Queries
{
    /// <summary>
    /// Query para consultar cesta Top Five atual.
    /// Operação de leitura - sem efeitos colaterais.
    /// </summary>
    public class ConsultarCestaQuery : IRequest<CestaResponse>
    {
    }
}
