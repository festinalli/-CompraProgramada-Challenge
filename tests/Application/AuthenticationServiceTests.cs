using System.Collections.Generic;
using Application.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.Application
{
    public class AuthenticationServiceTests
    {
        private static AuthenticationService Build()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JWT:SecretKey"] = "unit-test-secret-key-com-no-minimo-32-caracteres",
                ["JWT:Issuer"] = "corretora",
                ["JWT:Audience"] = "corretora",
                ["JWT:ExpirationMinutes"] = "60",
            }).Build();
            return new AuthenticationService(config, Mock.Of<ILogger<AuthenticationService>>());
        }

        [Fact]
        public void GerarToken_DeveProduzirTokenValido()
        {
            var svc = Build();
            var token = svc.GerarToken(1, "Ana", new[] { "Administrador" }, new[] { "cesta:ler", "motor:executar" });

            token.Should().NotBeNullOrEmpty();
            svc.ValidarToken(token).Should().BeTrue();
        }

        [Fact]
        public void ValidarToken_TokenAdulterado_DeveSerFalso()
        {
            var svc = Build();
            svc.ValidarToken("nao-e-um-jwt").Should().BeFalse();
        }
    }
}
