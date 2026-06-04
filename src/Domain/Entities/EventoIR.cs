using System;

namespace Domain.Entities
{
    public enum TipoIR
    {
        DEDO_DURO, // 0,005% sobre venda
        LUCRO_MENSAL // 20% sobre lucro se vendas > 20k
    }

    public class EventoIR
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public Cliente? Cliente { get; set; }
        // RN-056: a mensagem Kafka deve conter o CPF do cliente
        public string CPF { get; set; } = string.Empty;
        public TipoIR Tipo { get; set; }
        public decimal ValorBase { get; set; } // Valor da operacao ou lucro
        // Alíquota como fração decimal: 0.00005 (0,005% dedo-duro) ou 0.20 (20% lucro)
        public decimal Aliquota { get; set; }
        public decimal ValorImposto { get; set; }
        public DateTime DataEvento { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public bool PublicadoKafka { get; set; } = false;
    }
}
