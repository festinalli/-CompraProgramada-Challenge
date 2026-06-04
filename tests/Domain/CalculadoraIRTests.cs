using Domain.Services;
using FluentAssertions;
using Xunit;

namespace Tests.Domain
{
    public class CalculadoraIRTests
    {
        [Fact]
        public void DedoDuro_DeveSer_0005PorCento_DaOperacao()
        {
            // R$ 1.000 × 0,005% = R$ 0,05
            CalculadoraIR.DedoDuro(1000m).Should().Be(0.05m);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-100)]
        public void DedoDuro_OperacaoNaoPositiva_DeveSerZero(decimal valor)
        {
            CalculadoraIR.DedoDuro(valor).Should().Be(0m);
        }

        [Fact]
        public void IrLucro_VendasDoMesAte20k_DeveSerIsento()
        {
            // Vendas R$ 15.000 (≤ 20k), lucro R$ 2.000 → isento
            CalculadoraIR.IrSobreLucroMensal(totalVendasMes: 15000m, lucro: 2000m).Should().Be(0m);
        }

        [Fact]
        public void IrLucro_VendasDoMesAcima20k_Deve20PorCentoDoLucro()
        {
            // Vendas R$ 25.000 (> 20k), lucro R$ 5.000 → 20% = R$ 1.000
            CalculadoraIR.IrSobreLucroMensal(totalVendasMes: 25000m, lucro: 5000m).Should().Be(1000m);
        }

        [Fact]
        public void IrLucro_ComPrejuizo_DeveSerZero_MesmoAcimaDe20k()
        {
            CalculadoraIR.IrSobreLucroMensal(totalVendasMes: 30000m, lucro: -800m).Should().Be(0m);
        }

        [Fact]
        public void IrLucro_ExatamenteNoLimite_DeveSerIsento()
        {
            // Limite é "acima de" 20.000 → exatamente 20.000 ainda é isento
            CalculadoraIR.IrSobreLucroMensal(totalVendasMes: 20000m, lucro: 1000m).Should().Be(0m);
        }
    }
}
