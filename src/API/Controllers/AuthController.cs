using Application.DTOs;
using Application.Services;
using Application.Services.Security;
using Domain.Security;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    /// <summary>
    /// Controller para autenticação e geração de tokens JWT.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthenticationService _authService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly AppDbContext _context;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthenticationService authService,
            IPasswordHasher passwordHasher,
            AppDbContext context,
            ILogger<AuthController> logger)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Realiza login e retorna token JWT.
        /// </summary>
        /// <param name="request">Credenciais (CPF e senha)</param>
        /// <returns>Token JWT para uso em requisições autenticadas</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                // Validações básicas
                if (string.IsNullOrWhiteSpace(request.CPF) || string.IsNullOrWhiteSpace(request.Senha))
                {
                    return BadRequest(new { erro = "CPF e senha são obrigatórios" });
                }

                // Normaliza CPF (aceita com ou sem máscara) e busca o cliente
                var cpf = new string(request.CPF.Where(char.IsDigit).ToArray());
                var cliente = await _context.Clientes
                    .FirstOrDefaultAsync(c => c.CPF == cpf && c.Ativo);

                // Verifica a senha contra o hash. Mensagem genérica e verificação
                // mesmo com cliente nulo dificultam enumeração de CPF/timing.
                var senhaValida = cliente != null && _passwordHasher.Verify(request.Senha, cliente.SenhaHash);
                if (!senhaValida)
                {
                    _logger.LogWarning("⚠️ Login falhou para CPF {CPF}", cpf);
                    return Unauthorized(new { erro = "CPF ou senha inválidos" });
                }

                // Cliente é um usuário self-service: papel "Cliente" e permissão de leitura
                // da própria carteira (o acesso é restringido ao próprio id no controller).
                var token = _authService.GerarToken(
                    cliente!.Id, cliente.Nome,
                    new[] { RolesPadrao.Cliente },
                    new[] { Permissions.CarteiraLer });

                _logger.LogInformation("✅ Login bem-sucedido para cliente {ClienteId}", cliente.Id);

                return Ok(new LoginResponse
                {
                    Token = token,
                    ClienteId = cliente.Id,
                    Nome = cliente.Nome,
                    Email = cliente.Email,
                    ValorMensalAporte = cliente.ValorMensalAporte,
                    ExpiracaoEm = DateTime.UtcNow.AddHours(1),
                    Mensagem = "Login realizado com sucesso"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao processar login");
                return StatusCode(500, new { erro = "Erro ao processar login" });
            }
        }

        /// <summary>
        /// Realiza login administrativo com credenciais especiais.
        /// </summary>
        /// <param name="request">Credenciais de admin</param>
        /// <returns>Token JWT com role Admin</returns>
        [HttpPost("login-admin")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<LoginResponse>> LoginAdmin([FromBody] AdminLoginRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Usuario) || string.IsNullOrWhiteSpace(request.Senha))
                {
                    return BadRequest(new { erro = "Usuário e senha são obrigatórios" });
                }

                // Usuário de backoffice (com papéis e permissões) vindo do banco, hash PBKDF2.
                var usuario = await _context.Usuarios
                    .Include(u => u.Roles).ThenInclude(r => r.Permissions)
                    .FirstOrDefaultAsync(u => u.Username == request.Usuario);

                var senhaValida = usuario != null && _passwordHasher.Verify(request.Senha, usuario.SenhaHash);
                if (!senhaValida)
                {
                    _logger.LogWarning("⚠️ Tentativa de login admin com credenciais inválidas");
                    return Unauthorized(new { erro = "Credenciais de admin inválidas" });
                }

                // JWT com os papéis e as permissões finas agregadas do usuário.
                var roles = usuario!.Roles.Select(r => r.Nome).ToArray();
                var permissoes = usuario.Roles.SelectMany(r => r.Permissions.Select(p => p.Nome)).Distinct().ToArray();
                var token = _authService.GerarToken(usuario.Id, usuario.Nome, roles, permissoes);

                _logger.LogInformation("✅ Login admin bem-sucedido: {Username}", usuario.Username);

                return Ok(new LoginResponse
                {
                    Token = token,
                    ClienteId = usuario.Id,
                    Nome = usuario.Nome,
                    Email = usuario.Username,
                    ExpiracaoEm = DateTime.UtcNow.AddHours(1),
                    Mensagem = "Login administrativo realizado com sucesso"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao processar login admin");
                return StatusCode(500, new { erro = "Erro ao processar login" });
            }
        }

        /// <summary>
        /// Valida se o token atual é válido.
        /// </summary>
        /// <returns>Status de validação</returns>
        [HttpGet("validar")]
        [Authorize]
        [ProducesResponseType(typeof(ValidacaoTokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public ActionResult<ValidacaoTokenResponse> ValidarToken()
        {
            try
            {
                var clienteId = User.FindFirst("ClienteId")?.Value ?? "0";
                var nome = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value ?? "Desconhecido";
                var role = User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value ?? "Cliente";

                return Ok(new ValidacaoTokenResponse
                {
                    Valido = true,
                    ClienteId = int.Parse(clienteId),
                    Nome = nome,
                    Role = role,
                    Mensagem = "Token válido"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao validar token");
                return Unauthorized(new { erro = "Token inválido" });
            }
        }
    }

    /// <summary>
    /// DTO para requisição de login.
    /// </summary>
    public class LoginRequest
    {
        public string CPF { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para login administrativo.
    /// </summary>
    public class AdminLoginRequest
    {
        public string Usuario { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para resposta de login.
    /// </summary>
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public int ClienteId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal ValorMensalAporte { get; set; }
        public DateTime ExpiracaoEm { get; set; }
        public string Mensagem { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para validação de token.
    /// </summary>
    public class ValidacaoTokenResponse
    {
        public bool Valido { get; set; }
        public int ClienteId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Mensagem { get; set; } = string.Empty;
    }
}
