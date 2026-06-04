using System;
using System.Linq;

namespace Domain.Services
{
    /// <summary>
    /// Regra de datas do motor de compra (pura, testável):
    /// execução nos dias 5, 15 e 25; se caírem em fim de semana, no próximo dia útil.
    /// </summary>
    public static class CalendarioPregao
    {
        private static readonly int[] DiasAlvo = { 5, 15, 25 };

        public static bool EhDataDeExecucaoValida(DateTime data)
        {
            // Nunca executa em fim de semana.
            if (data.DayOfWeek == DayOfWeek.Saturday || data.DayOfWeek == DayOfWeek.Sunday)
                return false;

            // O próprio dia é 5/15/25 e é dia útil.
            if (DiasAlvo.Contains(data.Day))
                return true;

            // Segunda-feira que "herdou" um 5/15/25 caído no fim de semana:
            // sábado -> próximo útil é segunda (+2); domingo -> segunda (+1).
            if (data.DayOfWeek == DayOfWeek.Monday)
            {
                var sabado = data.AddDays(-2);
                var domingo = data.AddDays(-1);
                if (sabado.DayOfWeek == DayOfWeek.Saturday && DiasAlvo.Contains(sabado.Day))
                    return true;
                if (domingo.DayOfWeek == DayOfWeek.Sunday && DiasAlvo.Contains(domingo.Day))
                    return true;
            }

            return false;
        }
    }
}
