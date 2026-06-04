using Application.Abstractions;
using System;
using System.IO;
using FluentAssertions;
using Infrastructure.Parsers;
using Xunit;

namespace Tests.Infrastructure
{
    public class CotahistParserTests
    {
        private readonly ICotahistParser _parser = new CotahistParser();
        // Fixture COTAHIST real copiado para o output (ver Tests.csproj).
        private static readonly string Pasta = Path.Combine(AppContext.BaseDirectory, "cotacoes");

        [Theory]
        [InlineData("PETR4")]
        [InlineData("VALE3")]
        [InlineData("ITUB4")]
        [InlineData("BBDC4")]
        [InlineData("WEGE3")]
        public void ObterCotacaoFechamento_TickerDaCesta_DeveRetornarPrecoPositivo(string ticker)
        {
            var cotacao = _parser.ObterCotacaoFechamento(Pasta, ticker);

            cotacao.Should().NotBeNull($"o fixture B3 contém {ticker}");
            cotacao!.Ticker.Should().Be(ticker);
            cotacao.PrecoFechamento.Should().BeGreaterThan(0);
            cotacao.DataPregao.Should().NotBe(default(DateTime));
        }

        [Fact]
        public void ObterCotacaoFechamento_TickerInexistente_DeveRetornarNull()
        {
            _parser.ObterCotacaoFechamento(Pasta, "ZZZZ9").Should().BeNull();
        }

        [Fact]
        public void ObterCotacaoFechamento_PastaInexistente_DeveRetornarNull()
        {
            _parser.ObterCotacaoFechamento("/pasta/que/nao/existe", "PETR4").Should().BeNull();
        }

        [Fact]
        public void ParseArquivo_DeveIgnorarRegistrosForaDoMercadoAVistaEFracionario()
        {
            var arquivo = Directory.GetFiles(Pasta, "COTAHIST_D*.TXT")[0];

            var cotacoes = _parser.ParseArquivo(arquivo);

            cotacoes.Should().NotBeEmpty();
            cotacoes.Should().OnlyContain(c => c.TipoMercado == 10 || c.TipoMercado == 20);
        }
    }
}
