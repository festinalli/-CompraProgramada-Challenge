using System;

namespace Domain.Entities
{
    public enum TipoConta
    {
        MASTER,
        FILHOTE
    }

    public class ContaGrafica
    {
        public int Id { get; set; }
        public string NumeroConta { get; set; } = string.Empty;
        public TipoConta Tipo { get; set; }
        public DateTime DataCriacao { get; set; }
        public DateTime DataAbertura { get; set; }
        public bool Ativa { get; set; } = true;

        // Relacionamentos
        public int? ClienteId { get; set; }
        public Cliente? Cliente { get; set; }
    }
}
