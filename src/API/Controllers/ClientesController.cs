using API.Security;
using Application.DTOs;
using Application.Features.Clientes.Commands;
using Application.Features.Clientes.Queries;
using Domain.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// Controller para operações de clientes.
    /// Segue padrão CQRS com MediatR.
    /// Controllers apenas orquestram requisições para Handlers.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ClientesController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<ClientesController> _logger;

        public ClientesController(IMediator mediator, ILogger<ClientesController> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Defesa contra IDOR: um cliente só acessa o PRÓPRIO id; backoffice
        /// (Administrador/Operador) pode acessar qualquer cliente (suporte).
        /// </summary>
        private bool PodeAcessar(int clienteId)
        {
            if (User.IsInRole(RolesPadrao.Administrador) || User.IsInRole(RolesPadrao.Operador))
                return true;
            return User.FindFirst("ClienteId")?.Value == clienteId.ToString();
        }

        /// <summary>
        /// Realiza adesão de novo cliente ao programa de compra programada.
        /// </summary>
        /// <param name="request">Dados de adesão do cliente</param>
        /// <returns>Confirmação de adesão com dados mascarados</returns>
        [HttpPost("adesao")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AdesaoClienteResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<AdesaoClienteResponse>> Aderir([FromBody] AdesaoClienteRequest request)
        {
            try
            {
                var command = new AderirClienteCommand
                {
                    CPF = request.CPF,
                    Nome = request.Nome,
                    Email = request.Email,
                    ValorMensal = request.ValorMensal,
                    Senha = request.Senha
                };

                var response = await _mediator.Send(command);
                return CreatedAtAction(nameof(ConsultarCarteira), new { clienteId = response.ClienteId }, response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("⚠️ Erro de validação na adesão: {Mensagem}", ex.Message);
                return BadRequest(new { erro = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("⚠️ Operação inválida na adesão: {Mensagem}", ex.Message);
                return Conflict(new { erro = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro inesperado na adesão");
                return StatusCode(500, new { erro = "Erro ao processar adesão" });
            }
        }

        /// <summary>
        /// Consulta carteira consolidada do cliente.
        /// </summary>
        /// <param name="clienteId">ID do cliente</param>
        /// <returns>Carteira com posições e rentabilidade</returns>
        [HttpGet("{clienteId}/carteira")]
        [HasPermission(Permissions.CarteiraLer)]
        [ProducesResponseType(typeof(CarteiraClienteResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<CarteiraClienteResponse>> ConsultarCarteira(int clienteId)
        {
            if (!PodeAcessar(clienteId)) return Forbid();
            try
            {
                var query = new ConsultarCarteiraQuery { ClienteId = clienteId };
                var response = await _mediator.Send(query);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("⚠️ Cliente não encontrado: {Mensagem}", ex.Message);
                return NotFound(new { erro = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao consultar carteira do cliente {ClienteId}", clienteId);
                return StatusCode(500, new { erro = "Erro ao consultar carteira" });
            }
        }

        /// <summary>
        /// Consulta a rentabilidade detalhada da carteira do cliente
        /// (P/L por ativo, P/L total, rentabilidade % e composição real).
        /// </summary>
        /// <param name="clienteId">ID do cliente</param>
        [HttpGet("{clienteId}/rentabilidade")]
        [HasPermission(Permissions.CarteiraLer)]
        [ProducesResponseType(typeof(CarteiraClienteResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<CarteiraClienteResponse>> ConsultarRentabilidade(int clienteId)
        {
            if (!PodeAcessar(clienteId)) return Forbid();
            try
            {
                var response = await _mediator.Send(new ConsultarCarteiraQuery { ClienteId = clienteId });
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { erro = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao consultar rentabilidade do cliente {ClienteId}", clienteId);
                return StatusCode(500, new { erro = "Erro ao consultar rentabilidade" });
            }
        }

        /// <summary>
        /// Altera o valor mensal de aporte do cliente.
        /// </summary>
        [HttpPut("{clienteId}/valor-mensal")]
        [Authorize]
        [ProducesResponseType(typeof(AdesaoClienteResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AdesaoClienteResponse>> AlterarAporte(int clienteId, [FromBody] AlterarAporteRequest request)
        {
            if (!PodeAcessar(clienteId)) return Forbid();
            try
            {
                var command = new AlterarAporteCommand
                {
                    ClienteId = clienteId,
                    NovoValorMensalAporte = request.NovoValorMensalAporte
                };

                var response = await _mediator.Send(command);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("⚠️ Erro de validação ao alterar aporte: {Mensagem}", ex.Message);
                return BadRequest(new { erro = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("⚠️ Cliente não encontrado: {Mensagem}", ex.Message);
                return NotFound(new { erro = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao alterar aporte");
                return StatusCode(500, new { erro = "Erro ao alterar aporte" });
            }
        }

        /// <summary>
        /// Realiza saída do cliente do programa.
        /// </summary>
        /// <param name="request">Dados de saída</param>
        /// <returns>Confirmação de saída</returns>
        [HttpPost("{clienteId}/saida")]
        [Authorize]
        [ProducesResponseType(typeof(SaidaClienteResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SaidaClienteResponse>> Sair(int clienteId, [FromBody] SaidaClienteRequest request)
        {
            if (!PodeAcessar(clienteId)) return Forbid();
            try
            {
                var command = new SaidaClienteCommand
                {
                    ClienteId = clienteId,
                    Motivo = request.Motivo
                };

                var response = await _mediator.Send(command);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("⚠️ Cliente não encontrado: {Mensagem}", ex.Message);
                return NotFound(new { erro = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao processar saída do cliente {ClienteId}", clienteId);
                return StatusCode(500, new { erro = "Erro ao processar saída" });
            }
        }
    }
}
