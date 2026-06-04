using API.Security;
using Application.Services;
using Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MotorController : ControllerBase
    {
        private readonly IMotorCompraService _motorCompraService;

        public MotorController(IMotorCompraService motorCompraService)
        {
            _motorCompraService = motorCompraService;
        }

        [HttpPost("executar-compra")]
        [HasPermission(Permissions.MotorExecutar)]
        public async Task<IActionResult> ExecutarCompra([FromBody] ExecutarCompraRequest request)
        {
            try
            {
                await _motorCompraService.ExecutarCompraProgramada(request.DataReferencia);
                var status = await _motorCompraService.ObterStatusUltimaExecucao();
                return Ok(status ?? new StatusMotorCompraDto
                {
                    DataExecucao = DateTime.UtcNow,
                    Sucesso = true,
                    Mensagem = "Compra programada executada (nenhuma ordem gerada)."
                });
            }
            catch (InvalidOperationException ex)
            {
                // Dia inválido ou execução já feita para a data (idempotência).
                return BadRequest(new { erro = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = ex.Message });
            }
        }
    }

    public class ExecutarCompraRequest { public DateTime DataReferencia { get; set; } }
}
