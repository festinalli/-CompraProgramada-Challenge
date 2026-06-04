using Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Tests.Domain
{
    public class CustodiaTests
    {
        [Fact]
        public void AtualizarPrecoMedio_DuasCompras_DeveSerMediaPonderada()
        {
            var c = new CustodiaFilhote { Ticker = "PETR4" };

            c.AtualizarPrecoMedio(100, 20m); // 100 @ 20
            c.AtualizarPrecoMedio(100, 22m); // +100 @ 22

            c.Quantidade.Should().Be(200);
            c.PrecoMedio.Should().Be(21m); // (100*20 + 100*22)/200
        }

        [Fact]
        public void RegistrarVenda_NaoAlteraPrecoMedio()
        {
            var c = new CustodiaFilhote { Ticker = "VALE3" };
            c.AtualizarPrecoMedio(100, 50m);

            c.RegistrarVenda(40);

            c.Quantidade.Should().Be(60);
            c.PrecoMedio.Should().Be(50m); // venda não muda o preço médio
        }

        [Fact]
        public void CustodiaMaster_Atualizar_DeveConsolidarLoteEFracionario()
        {
            var m = new CustodiaMaster { Ticker = "ITUB4" };
            m.Atualizar(250, 30m, "teste"); // 250 ações

            m.Quantidade.Should().Be(250);
            m.QuantidadeLotePadrao.Should().Be(200); // múltiplos de 100
            m.QuantidadeFracionario.Should().Be(50);  // resto
            m.PrecoMedio.Should().Be(30m);
        }
    }
}
