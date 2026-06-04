using Application.Abstractions;
using System;
using System.Linq;
using System.Threading.Tasks;
using Application.Services;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Messaging;
using Infrastructure.Parsers;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Tests.Application
{
    /// <summary>
    /// Testes reais do motor: exercitam a lógica de produção com um parser que
    /// devolve cotação e asserções sobre ordens, lote/fracionário, dedução do
    /// saldo master, distribuição proporcional e IR/Kafka.
    /// </summary>
    public class MotorCompraServiceTests
    {
        private const string DiaValido = "2026-03-05"; // quinta-feira, dia 5

        private static AppDbContext NewContext() =>
            new(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        private static Mock<ICotahistParser> ParserPreco(decimal preco)
        {
            var mock = new Mock<ICotahistParser>();
            mock.Setup(p => p.ObterCotacaoFechamento(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string _, string t) => new CotacaoB3 { Ticker = t, PrecoFechamento = preco });
            return mock;
        }

        private static async Task SeedCestaUmAtivo(AppDbContext ctx, string ticker)
        {
            ctx.CestasRecomendacao.Add(new CestaRecomendacao
            {
                Nome = "T", Ativa = true, DataCriacao = DateTime.UtcNow,
                Itens = { new ItemCesta { Ticker = ticker, Percentual = 100 } }
            });
            await ctx.SaveChangesAsync();
        }

        private static async Task<Cliente> SeedCliente(AppDbContext ctx, decimal aporteMensal)
        {
            var c = new Cliente { Nome = "C", CPF = Guid.NewGuid().ToString("N")[..11], Email = "c@c", SenhaHash = "h", ValorMensalAporte = aporteMensal, Ativo = true };
            ctx.Clientes.Add(c);
            await ctx.SaveChangesAsync();
            return c;
        }

        private static MotorCompraService Build(AppDbContext ctx, Mock<ICotahistParser> parser, IKafkaProducer? kafka = null) =>
            new(ctx, parser.Object, kafka ?? Mock.Of<IKafkaProducer>(), "cotacoes");

        [Fact]
        public async Task Motor_SeparaLotePadraoEFracionarioComSufixoF()
        {
            var ctx = NewContext();
            await SeedCestaUmAtivo(ctx, "PETR4");
            await SeedCliente(ctx, aporteMensal: 1050m); // 1/3 = 350 @ preço 1 → 350 ações
            await Build(ctx, ParserPreco(1m)).ExecutarCompraProgramada(DateTime.Parse(DiaValido));

            var ordem = ctx.OrdensCompra.Include(o => o.Detalhes).Single();
            var padrao = ordem.Detalhes.Single(d => d.Mercado == Mercado.PADRAO);
            var frac = ordem.Detalhes.Single(d => d.Mercado == Mercado.FRACIONARIO);

            padrao.Quantidade.Should().Be(300);          // múltiplo de 100
            padrao.TickerExecutado.Should().Be("PETR4");
            frac.Quantidade.Should().Be(50);             // resto 1–99
            frac.TickerExecutado.Should().Be("PETR4F");  // sufixo "F"
        }

        [Fact]
        public async Task Motor_DeduzSaldoDaCustodiaMasterAntesDeComprar()
        {
            var ctx = NewContext();
            await SeedCestaUmAtivo(ctx, "PETR4");
            await SeedCliente(ctx, aporteMensal: 1050m); // precisa de 350
            ctx.CustodiasMaster.Add(new CustodiaMaster { Ticker = "PETR4", Quantidade = 100, PrecoMedio = 1m });
            await ctx.SaveChangesAsync();

            await Build(ctx, ParserPreco(1m)).ExecutarCompraProgramada(DateTime.Parse(DiaValido));

            // Compra apenas 250 (350 necessárias − 100 do master)
            ctx.OrdensCompra.Single().QuantidadeTotal.Should().Be(250);
        }

        [Fact]
        public async Task Motor_DistribuiProporcionalmenteAosFilhotes()
        {
            var ctx = NewContext();
            await SeedCestaUmAtivo(ctx, "PETR4");
            var c1 = await SeedCliente(ctx, aporteMensal: 600m); // 1/3 = 200 (proporção 2/3)
            var c2 = await SeedCliente(ctx, aporteMensal: 300m); // 1/3 = 100 (proporção 1/3) → 300 ações
            await Build(ctx, ParserPreco(1m)).ExecutarCompraProgramada(DateTime.Parse(DiaValido));

            // Distribuição proporcional com TRUNCAMENTO (RN-036): 2/3×300=200; 1/3×300=99,99→99
            ctx.CustodiasFilhotes.Single(x => x.ClienteId == c1.Id).Quantidade.Should().Be(200);
            ctx.CustodiasFilhotes.Single(x => x.ClienteId == c2.Id).Quantidade.Should().Be(99);
            // A ação não distribuída (resíduo) permanece na custódia master (RN-039)
            ctx.CustodiasMaster.Single(m => m.Ticker == "PETR4").Quantidade.Should().Be(1);
        }

        [Fact]
        public async Task Motor_PublicaIRDedoDuroNoKafka()
        {
            var ctx = NewContext();
            await SeedCestaUmAtivo(ctx, "PETR4");
            await SeedCliente(ctx, aporteMensal: 300m);
            var kafka = new Mock<IKafkaProducer>();

            await Build(ctx, ParserPreco(1m), kafka.Object).ExecutarCompraProgramada(DateTime.Parse(DiaValido));

            ctx.EventosIR.Should().Contain(e => e.Tipo == TipoIR.DEDO_DURO);
            kafka.Verify(k => k.PublicarEventoIR(It.Is<EventoIR>(e => e.Tipo == TipoIR.DEDO_DURO)), Times.AtLeastOnce);
        }

        [Fact]
        public async Task Motor_DiaInvalido_DeveLancar()
        {
            var ctx = NewContext();
            await SeedCestaUmAtivo(ctx, "PETR4");
            await SeedCliente(ctx, 300m);
            var act = () => Build(ctx, ParserPreco(1m)).ExecutarCompraProgramada(DateTime.Parse("2026-03-07")); // sábado
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task Motor_NaoEhIdempotente_SegundaExecucaoNaMesmaDataLanca()
        {
            var ctx = NewContext();
            await SeedCestaUmAtivo(ctx, "PETR4");
            await SeedCliente(ctx, 300m);
            var svc = Build(ctx, ParserPreco(1m));

            await svc.ExecutarCompraProgramada(DateTime.Parse(DiaValido));
            var act = () => svc.ExecutarCompraProgramada(DateTime.Parse(DiaValido));
            await act.Should().ThrowAsync<InvalidOperationException>(); // já executado para a data
        }
    }
}
