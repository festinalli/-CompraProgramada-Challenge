using System;
using System.Collections.Generic;

namespace Domain.Entities
{
    public enum TipoOrdem
    {
        COMPRA,
        VENDA
    }

    public enum Mercado
    {
        PADRAO,
        FRACIONARIO
    }

    public class OrdemCompra
    {
        public int Id { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public int QuantidadeTotal { get; set; }
        public decimal PrecoUnitario { get; set; }
        public decimal ValorTotal { get; set; }
        public TipoOrdem Tipo { get; set; }
        public DateTime DataExecucao { get; set; }
        public string Referencia { get; set; } = string.Empty; // Ex: "Compra Programada 2026-02-05"

        // Detalhes da execucao (Lote Padrao vs Fracionario)
        public ICollection<DetalheExecucaoOrdem> Detalhes { get; set; } = new List<DetalheExecucaoOrdem>();
    }

    public class DetalheExecucaoOrdem
    {
        public int Id { get; set; }
        public int OrdemCompraId { get; set; }
        public OrdemCompra? OrdemCompra { get; set; }
        public Mercado Mercado { get; set; }
        public string TickerExecutado { get; set; } = string.Empty; // Ex: PETR4 ou PETR4F
        public int Quantidade { get; set; }
    }
}
