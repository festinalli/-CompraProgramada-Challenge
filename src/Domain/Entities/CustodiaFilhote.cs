using System;

namespace Domain.Entities
{
    public class CustodiaFilhote
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public Cliente? Cliente { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public int Quantidade { get; set; }
        public decimal PrecoMedio { get; set; }
        public decimal ValorAtual => Quantidade * PrecoMedio;
        public DateTime DataAquisicao { get; set; }
        public DateTime DataUltimaAtualizacao { get; set; }

        // Metodo para atualizar preco medio em compras
        public void AtualizarPrecoMedio(int novaQuantidade, decimal precoCompra)
        {
            if (Quantidade + novaQuantidade == 0) return;
            
            PrecoMedio = (Quantidade * PrecoMedio + novaQuantidade * precoCompra) / (Quantidade + novaQuantidade);
            Quantidade += novaQuantidade;
            DataUltimaAtualizacao = DateTime.UtcNow;
        }

        // Metodo para atualizar quantidade em vendas (preco medio nao altera)
        public void RegistrarVenda(int quantidadeVendida)
        {
            Quantidade -= quantidadeVendida;
            DataUltimaAtualizacao = DateTime.UtcNow;
        }
    }
}
