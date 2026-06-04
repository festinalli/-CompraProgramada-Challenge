using System;
using System.Collections.Generic;

namespace Application.Abstractions
{
    /// <summary>Cotação de um ativo extraída do arquivo COTAHIST da B3.</summary>
    public class CotacaoB3
    {
        public DateTime DataPregao { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public string CodigoBDI { get; set; } = string.Empty;
        public int TipoMercado { get; set; }
        public string NomeEmpresa { get; set; } = string.Empty;
        public decimal PrecoAbertura { get; set; }
        public decimal PrecoMaximo { get; set; }
        public decimal PrecoMinimo { get; set; }
        public decimal PrecoFechamento { get; set; }
        public decimal PrecoMedio { get; set; }
        public long QuantidadeNegociada { get; set; }
        public decimal VolumeNegociado { get; set; }
    }

    /// <summary>Parser de arquivos COTAHIST da B3 (abstração — implementada na Infraestrutura).</summary>
    public interface ICotahistParser
    {
        IEnumerable<CotacaoB3> ParseArquivo(string caminhoArquivo);
        CotacaoB3? ObterCotacaoAtiva(string ticker, string caminhoArquivo);
        CotacaoB3? ObterCotacaoFechamento(string pastaCotacoes, string ticker);
    }

    /// <summary>Fornece a cotação atual (último fechamento) de um ticker.</summary>
    public interface ICotacaoProvider
    {
        decimal? ObterPrecoAtual(string ticker);
    }
}
