using Application.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Services;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Messaging;
using Infrastructure.Parsers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.Application
{
    public class RebalanceamentoServiceTests
    {
        private static AppDbContext NewContext() =>
            new(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        private static Mock<ICotahistParser> ParserComPrecos(Func<string, decimal> preco)
        {
            var mock = new Mock<ICotahistParser>();
            mock.Setup(p => p.ObterCotacaoFechamento(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string _, string t) => new CotacaoB3 { Ticker = t, PrecoFechamento = preco(t) });
            return mock;
        }

        private static CestaRecomendacao NovaCesta(params string[] tickers) => new()
        {
            Nome = "NOVA",
            Ativa = true,
            Itens = tickers.Select(t => new ItemCesta { Ticker = t, Percentual = 20 }).ToList()
        };

        private static RebalanceamentoService BuildService(AppDbContext ctx, Mock<ICotahistParser> parser) =>
            new(ctx, parser.Object, Mock.Of<IKafkaProducer>(),
                Mock.Of<ILogger<RebalanceamentoService>>(), "cotacoes");

        private static async Task<Cliente> SeedClienteComCustodia(AppDbContext ctx, string ticker, int qtd, decimal pm)
        {
            var cliente = new Cliente { Nome = "C", CPF = "1", Email = "c@c", SenhaHash = "h", ValorMensalAporte = 1000, Ativo = true };
            ctx.Clientes.Add(cliente);
            await ctx.SaveChangesAsync();
            ctx.CustodiasFilhotes.Add(new CustodiaFilhote { ClienteId = cliente.Id, Ticker = ticker, Quantidade = qtd, PrecoMedio = pm });
            await ctx.SaveChangesAsync();
            return cliente;
        }

        [Fact]
        public async Task MudancaCesta_VendeAtivoQueSaiu()
        {
            var ctx = NewContext();
            var cliente = await SeedClienteComCustodia(ctx, "OLDX3", 100, 10m);
            var svc = BuildService(ctx, ParserComPrecos(_ => 12m));

            await svc.RebalancearPorMudancaCesta(cliente.Id, NovaCesta("AAAA3", "BBBB3", "CCCC3", "DDDD3", "EEEE3"));

            ctx.CustodiasFilhotes.Any(c => c.Ticker == "OLDX3" && c.ClienteId == cliente.Id).Should().BeFalse();
            ctx.MovimentacoesVenda.Should().ContainSingle(v => v.Ticker == "OLDX3" && v.Quantidade == 100);
        }

        [Fact]
        public async Task MudancaCesta_VendasDoMesAte20k_NaoGeraIRDeLucro()
        {
            var ctx = NewContext();
            // 100 @ pm 10 vendidas a 50 → venda 5.000 (≤ 20k), lucro 4.000 → isento
            var cliente = await SeedClienteComCustodia(ctx, "OLDX3", 100, 10m);
            var svc = BuildService(ctx, ParserComPrecos(t => t == "OLDX3" ? 50m : 25m));

            await svc.RebalancearPorMudancaCesta(cliente.Id, NovaCesta("AAAA3", "BBBB3", "CCCC3", "DDDD3", "EEEE3"));

            ctx.EventosIR.Any(e => e.Tipo == TipoIR.LUCRO_MENSAL).Should().BeFalse();
        }

        [Fact]
        public async Task MudancaCesta_VendasDoMesAcima20k_Gera20PorCentoDoLucro()
        {
            var ctx = NewContext();
            // 1000 @ pm 10 vendidas a 30 → venda 30.000 (> 20k), lucro 20.000 → IR 4.000
            var cliente = await SeedClienteComCustodia(ctx, "OLDX3", 1000, 10m);
            var svc = BuildService(ctx, ParserComPrecos(t => t == "OLDX3" ? 30m : 25m));

            await svc.RebalancearPorMudancaCesta(cliente.Id, NovaCesta("AAAA3", "BBBB3", "CCCC3", "DDDD3", "EEEE3"));

            var ir = ctx.EventosIR.SingleOrDefault(e => e.Tipo == TipoIR.LUCRO_MENSAL);
            ir.Should().NotBeNull();
            ir!.ValorImposto.Should().Be(4000m); // 20% de 20.000
        }
    }
}
