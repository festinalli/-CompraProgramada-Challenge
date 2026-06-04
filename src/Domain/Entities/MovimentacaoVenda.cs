using System;

namespace Domain.Entities
{
    /// <summary>
    /// Registro de uma venda de ativo (rebalanceamento). Serve de ledger para
    /// agregar as vendas do mês e aplicar a regra de IR (isenção até R$ 20.000/mês).
    /// </summary>
    public class MovimentacaoVenda
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public int Quantidade { get; set; }
        public decimal ValorVenda { get; set; }
        public decimal Lucro { get; set; }
        public DateTime Data { get; set; }
        public int Ano { get; set; }
        public int Mes { get; set; }
    }
}
