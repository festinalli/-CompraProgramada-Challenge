using System;
using System.Collections.Concurrent;
using Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Parsers
{
    /// <summary>
    /// Fornece a cotação atual (preço de fechamento do último pregão disponível)
    /// de um ticker, reusando o parser COTAHIST. Mantém um cache simples em memória
    /// para não reparsear o arquivo a cada consulta de carteira.
    /// </summary>
    public class CotacaoProvider : ICotacaoProvider
    {
        private readonly ICotahistParser _parser;
        private readonly string _pastaCotacoes;
        private readonly ConcurrentDictionary<string, decimal?> _cache = new(StringComparer.OrdinalIgnoreCase);

        public CotacaoProvider(ICotahistParser parser, IConfiguration configuration)
        {
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _pastaCotacoes = configuration["Cotacoes:PastaLocal"] ?? "cotacoes";
        }

        public decimal? ObterPrecoAtual(string ticker)
        {
            if (string.IsNullOrWhiteSpace(ticker))
                return null;

            return _cache.GetOrAdd(ticker.Trim(), t =>
                _parser.ObterCotacaoFechamento(_pastaCotacoes, t)?.PrecoFechamento);
        }
    }
}
