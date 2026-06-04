using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    /// <summary>
    /// Serviço de autenticação JWT para geração e validação de tokens.
    /// Implementa segurança de nível corporativo.
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>Gera o JWT com papéis (role) e permissões finas (claim "permission").</summary>
        string GerarToken(int subjectId, string nome, IEnumerable<string> roles, IEnumerable<string> permissoes);
        bool ValidarToken(string token);
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expirationMinutes;
        private readonly ILogger<AuthenticationService> _logger;

        public AuthenticationService(
            IConfiguration configuration,
            ILogger<AuthenticationService> logger)
        {
            _secretKey = configuration["JWT:SecretKey"] 
                ?? throw new InvalidOperationException("JWT:SecretKey não configurado");
            _issuer = configuration["JWT:Issuer"] 
                ?? throw new InvalidOperationException("JWT:Issuer não configurado");
            _audience = configuration["JWT:Audience"] 
                ?? throw new InvalidOperationException("JWT:Audience não configurado");
            _expirationMinutes = int.Parse(configuration["JWT:ExpirationMinutes"] ?? "60");
            _logger = logger;

            // Validar tamanho da chave
            if (_secretKey.Length < 32)
            {
                throw new InvalidOperationException("JWT:SecretKey deve ter no mínimo 32 caracteres");
            }
        }

        /// <summary>
        /// Gera token JWT com claims de cliente.
        /// </summary>
        public string GerarToken(int subjectId, string nome, IEnumerable<string> roles, IEnumerable<string> permissoes)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_secretKey);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, subjectId.ToString()),
                    new Claim(ClaimTypes.Name, nome),
                    new Claim("ClienteId", subjectId.ToString()),
                    new Claim("IssuedAt", DateTime.UtcNow.ToString("O"))
                };
                foreach (var role in roles ?? Enumerable.Empty<string>())
                    claims.Add(new Claim(ClaimTypes.Role, role));
                foreach (var permissao in (permissoes ?? Enumerable.Empty<string>()).Distinct())
                    claims.Add(new Claim("permission", permissao));

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddMinutes(_expirationMinutes),
                    Issuer = _issuer,
                    Audience = _audience,
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                _logger.LogInformation("✅ Token gerado para subject {SubjectId}", subjectId);
                return tokenString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao gerar token para subject {SubjectId}", subjectId);
                throw;
            }
        }

        /// <summary>
        /// Valida um token JWT.
        /// </summary>
        public bool ValidarToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_secretKey);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("⚠️ Token inválido: {Mensagem}", ex.Message);
                return false;
            }
        }
    }
}
