using System;

namespace Domain.Entities
{
    public class CustodiaMaster
    {
        public int Id { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public int Quantidade { get; set; }
        public int QuantidadeLotePadrao => (Quantidade / 100) * 100;
        public int QuantidadeFracionario => Quantidade % 100;
        public decimal PrecoMedio { get; set; }
        public decimal ValorAtual => Quantidade * PrecoMedio;
        public DateTime DataUltimaAtualizacao { get; set; }
        public string Origem { get; set; } = string.Empty;

        public void Atualizar(int novaQuantidade, decimal precoCompra, string origem)
        {
            if (Quantidade + novaQuantidade == 0) return;
            
            PrecoMedio = (Quantidade * PrecoMedio + novaQuantidade * precoCompra) / (Quantidade + novaQuantidade);
            Quantidade += novaQuantidade;
            Origem = origem;
            DataUltimaAtualizacao = DateTime.UtcNow;
        }
    }
}
