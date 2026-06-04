using API.Security;
using Application.DTOs;
using Application.Features.Admin.Commands;
using Application.Features.Admin.Queries;
using Domain.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// Controller para operações administrativas.
    /// Requer autorização com role "Admin".
    /// Segue padrão CQRS com MediatR.
    /// Controllers apenas orquestram requisições para Handlers.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AdminController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IMediator mediator, ILogger<AdminController> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Cadastra a cesta Top Five de ativos recomendados.
        /// Operação crítica - apenas Admin.
        /// Valida que exatamente 5 ativos com soma de percentuais = 100%.
        /// </summary>
        /// <param name="request">Cesta com 5 ativos e percentuais</param>
        /// <returns>Confirmação do cadastro</returns>
        [HttpPost("cesta")]
        [HasPermission(Permissions.CestaEscrever)]
        [ProducesResponseType(typeof(CestaResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<CestaResponse>> CadastrarCesta([FromBody] CadastrarCestaRequest request)
        {
            try
            {
                var command = new CadastrarCestaCommand
                {
                    Nome = request.Nome,
                    Ativos = request.Ativos
                        .Select(a => new ItemCestaRequest
                        {
                            Ticker = a.Ticker,
                            Percentual = a.Percentual
                        })
                        .ToList()
                };

                var response = await _mediator.Send(command);
                return CreatedAtAction(nameof(ConsultarCesta), response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("⚠️ Erro de validação ao cadastrar cesta: {Mensagem}", ex.Message);
                return BadRequest(new { erro = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao cadastrar cesta");
                return StatusCode(500, new { erro = "Erro ao cadastrar cesta" });
            }
        }

        /// <summary>
        /// Consulta a cesta Top Five ativa.
        /// </summary>
        /// <returns>Cesta com ativos e percentuais</returns>
        [HttpGet("cesta/atual")]
        [HasPermission(Permissions.CestaLer)]
        [ProducesResponseType(typeof(CestaResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<CestaResponse>> ConsultarCesta()
        {
            try
            {
                var query = new ConsultarCestaQuery();
                var response = await _mediator.Send(query);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("⚠️ Cesta não encontrada: {Mensagem}", ex.Message);
                return NotFound(new { erro = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao consultar cesta");
                return StatusCode(500, new { erro = "Erro ao consultar cesta" });
            }
        }

        /// <summary>
        /// Histórico de cestas (ativa e desativadas), mais recente primeiro.
        /// </summary>
        [HttpGet("cesta/historico")]
        [HasPermission(Permissions.CestaLer)]
        [ProducesResponseType(typeof(List<CestaHistoricoResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<List<CestaHistoricoResponse>>> ConsultarHistoricoCestas()
        {
            var response = await _mediator.Send(new ConsultarHistoricoCestasQuery());
            return Ok(response);
        }

        /// <summary>
        /// Custódia master: resíduos consolidados (lote padrão e fracionário).
        /// </summary>
        [HttpGet("conta-master/custodia")]
        [HasPermission(Permissions.CustodiaLer)]
        [ProducesResponseType(typeof(List<CustodiaMasterResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<List<CustodiaMasterResponse>>> ConsultarCustodiaMaster()
        {
            var response = await _mediator.Send(new ConsultarCustodiaMasterQuery());
            return Ok(response);
        }
    }
}
