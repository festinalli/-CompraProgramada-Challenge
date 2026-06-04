using System.Collections.Generic;
using MediatR;
using Application.DTOs;

namespace Application.Features.Admin.Queries
{
    /// <summary>Query para o histórico de cestas (ativas e desativadas).</summary>
    public class ConsultarHistoricoCestasQuery : IRequest<List<CestaHistoricoResponse>>
    {
    }
}
