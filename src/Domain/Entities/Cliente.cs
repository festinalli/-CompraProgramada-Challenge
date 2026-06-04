using System;
using System.Collections.Generic;

namespace Domain.Entities
{
    public class Cliente
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string CPF { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string SenhaHash { get; set; } = string.Empty;
        public decimal ValorMensalAporte { get; set; }
        public bool Ativo { get; set; } = true;
        public DateTime DataAdesao { get; set; }
        public DateTime? DataSaida { get; set; }
        public string? MotivoSaida { get; set; }

        // Relacionamentos
        public ContaGrafica? ContaGrafica { get; set; }
        public ICollection<CustodiaFilhote> Custodias { get; set; } = new List<CustodiaFilhote>();
        public ICollection<HistoricoAporte> HistoricoAportes { get; set; } = new List<HistoricoAporte>();
        public ICollection<EventoIR> EventosIR { get; set; } = new List<EventoIR>();
    }
}
