using Application.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Features.Clientes.Handlers;
using Application.Features.Clientes.Queries;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Parsers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.Application
{
    public class ConsultarCarteiraQueryHandlerTests
    {
        private static AppDbContext NewContext() =>
            new(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        [Fact]
        public async Task Carteira_DeveCalcular_PL_E_Composicao_ComCotacaoAtual()
        {
            var ctx = NewContext();
            var cliente = new Cliente { Nome = "Ana", CPF = "1", Email = "a@a", SenhaHash = "h", ValorMensalAporte = 1000, Ativo = true };
            ctx.Clientes.Add(cliente);
            await ctx.SaveChangesAsync();
            ctx.CustodiasFilhotes.Add(new CustodiaFilhote { ClienteId = cliente.Id, Ticker = "PETR4", Quantidade = 100, PrecoMedio = 20m });
            await ctx.SaveChangesAsync();

            var cotacao = new Mock<ICotacaoProvider>();
            cotacao.Setup(c => c.ObterPrecoAtual("PETR4")).Returns(25m); // valorizou

            var handler = new ConsultarCarteiraQueryHandler(ctx, cotacao.Object, Mock.Of<ILogger<ConsultarCarteiraQueryHandler>>());

            var r = await handler.Handle(new ConsultarCarteiraQuery { ClienteId = cliente.Id }, CancellationToken.None);

            r.Posicoes.Should().HaveCount(1);
            var p = r.Posicoes[0];
            p.CotacaoAtual.Should().Be(25m);
            p.ValorAtual.Should().Be(2500m);          // 100 × 25
            p.Rentabilidade.Should().Be(500m);         // 2500 − (100 × 20)
            p.PercentualCarteira.Should().Be(100m);    // único ativo
            r.ValorInvestido.Should().Be(2000m);
            r.Rentabilidade.Should().Be(500m);
            r.PercentualRentabilidade.Should().Be(25m); // 500/2000
        }

        [Fact]
        public async Task Carteira_SemCotacao_UsaPrecoMedio_RentabilidadeZero()
        {
            var ctx = NewContext();
            var cliente = new Cliente { Nome = "Bia", CPF = "2", Email = "b@b", SenhaHash = "h", ValorMensalAporte = 1000, Ativo = true };
            ctx.Clientes.Add(cliente);
            await ctx.SaveChangesAsync();
            ctx.CustodiasFilhotes.Add(new CustodiaFilhote { ClienteId = cliente.Id, Ticker = "XPTO3", Quantidade = 10, PrecoMedio = 7m });
            await ctx.SaveChangesAsync();

            var cotacao = new Mock<ICotacaoProvider>();
            cotacao.Setup(c => c.ObterPrecoAtual(It.IsAny<string>())).Returns((decimal?)null); // sem cotação

            var handler = new ConsultarCarteiraQueryHandler(ctx, cotacao.Object, Mock.Of<ILogger<ConsultarCarteiraQueryHandler>>());

            var r = await handler.Handle(new ConsultarCarteiraQuery { ClienteId = cliente.Id }, CancellationToken.None);

            r.Posicoes[0].CotacaoAtual.Should().Be(7m);
            r.Rentabilidade.Should().Be(0m);
        }
    }
}
