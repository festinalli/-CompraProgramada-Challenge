using System;
using Domain.Services;
using FluentAssertions;
using Xunit;

namespace Tests.Domain
{
    public class CalendarioPregaoTests
    {
        [Theory]
        // 2026-01: dia 5 = segunda, 15 = quinta, 26 = segunda (25 cai domingo)
        [InlineData("2026-01-05", true)]   // dia 5 útil
        [InlineData("2026-01-15", true)]   // dia 15 útil
        [InlineData("2026-01-06", false)]  // dia comum
        [InlineData("2026-01-10", false)]  // sábado
        public void DiaUtilDireto(string data, bool esperado)
        {
            CalendarioPregao.EhDataDeExecucaoValida(DateTime.Parse(data)).Should().Be(esperado);
        }

        [Fact]
        public void Dia25NoDomingo_NaoExecutaNoDomingo_MasExecutaNaSegunda()
        {
            // 2026-01-25 é domingo
            DateTime.Parse("2026-01-25").DayOfWeek.Should().Be(DayOfWeek.Sunday);
            CalendarioPregao.EhDataDeExecucaoValida(DateTime.Parse("2026-01-25")).Should().BeFalse();
            // 2026-01-26 (segunda) é o próximo dia útil
            CalendarioPregao.EhDataDeExecucaoValida(DateTime.Parse("2026-01-26")).Should().BeTrue();
        }

        [Fact]
        public void Dia15NoSabado_ExecutaNaSegunda17()
        {
            // 2025-02-15 é sábado
            DateTime.Parse("2025-02-15").DayOfWeek.Should().Be(DayOfWeek.Saturday);
            CalendarioPregao.EhDataDeExecucaoValida(DateTime.Parse("2025-02-15")).Should().BeFalse();
            // 2025-02-17 (segunda) herda o dia 15 caído no fim de semana
            CalendarioPregao.EhDataDeExecucaoValida(DateTime.Parse("2025-02-17")).Should().BeTrue();
        }

        [Fact]
        public void SegundaComum_SemHeranca_NaoEhValida()
        {
            // 2026-01-12 é segunda, mas dias 10/11 não são alvo
            CalendarioPregao.EhDataDeExecucaoValida(DateTime.Parse("2026-01-12")).Should().BeFalse();
        }
    }
}
