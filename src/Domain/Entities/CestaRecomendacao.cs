using System;
using System.Collections.Generic;

namespace Domain.Entities
{
    public class CestaRecomendacao
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public bool Ativa { get; set; } = true;
        public DateTime DataCriacao { get; set; }
        public DateTime? DataDesativacao { get; set; }

        // Relacionamentos
        public ICollection<ItemCesta> Itens { get; set; } = new List<ItemCesta>();
    }

    public class ItemCesta
    {
        public int Id { get; set; }
        public int CestaId { get; set; }
        public CestaRecomendacao? Cesta { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public decimal Percentual { get; set; }
    }
}
