using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Features.Clientes.Commands;
using Application.Features.Clientes.Handlers;
using Application.Services.Security;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.Application
{
    public class AderirClienteHandlerTests
    {
        private static AppDbContext NewContext() =>
            new(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static AderirClienteCommandHandler Build(AppDbContext ctx) =>
            new(ctx, new Pbkdf2PasswordHasher(), Mock.Of<ILogger<AderirClienteCommandHandler>>());

        private static AderirClienteCommand Cmd(string cpf) => new()
        {
            CPF = cpf, Nome = "Fulano", Email = "f@f.com", ValorMensal = 500m, Senha = "Senha123"
        };

        [Fact]
        public async Task Adesao_Valida_PersisteClienteComHash_NaoPlaintext()
        {
            var ctx = NewContext();
            var resp = await Build(ctx).Handle(Cmd("52998224725"), CancellationToken.None);

            resp.ClienteId.Should().BeGreaterThan(0);
            var cliente = await ctx.Clientes.FirstAsync();
            cliente.SenhaHash.Should().NotBe("Senha123");
            new Pbkdf2PasswordHasher().Verify("Senha123", cliente.SenhaHash).Should().BeTrue();
            (await ctx.ContasGraficas.CountAsync()).Should().Be(1); // conta filhote criada
        }

        [Fact]
        public async Task Adesao_CpfInvalido_DeveLancarArgumentException()
        {
            var ctx = NewContext();
            var act = () => Build(ctx).Handle(Cmd("11111111111"), CancellationToken.None);
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task Adesao_CpfDuplicado_DeveLancarInvalidOperation()
        {
            var ctx = NewContext();
            ctx.Clientes.Add(new Cliente { Nome = "X", CPF = "52998224725", Email = "x@x", SenhaHash = "h", ValorMensalAporte = 100, Ativo = true });
            await ctx.SaveChangesAsync();

            var act = () => Build(ctx).Handle(Cmd("52998224725"), CancellationToken.None);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task Adesao_ValorAbaixoDoMinimo_DeveLancarArgumentException()
        {
            var ctx = NewContext();
            var cmd = Cmd("52998224725");
            cmd.ValorMensal = 50m; // mínimo é R$ 100
            var act = () => Build(ctx).Handle(cmd, CancellationToken.None);
            await act.Should().ThrowAsync<ArgumentException>();
        }
    }
}
