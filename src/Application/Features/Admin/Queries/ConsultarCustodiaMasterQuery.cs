using System.Collections.Generic;
using MediatR;
using Application.DTOs;

namespace Application.Features.Admin.Queries
{
    /// <summary>Query para a custódia master (resíduos consolidados).</summary>
    public class ConsultarCustodiaMasterQuery : IRequest<List<CustodiaMasterResponse>>
    {
    }
}
