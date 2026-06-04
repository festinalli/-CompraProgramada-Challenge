using Application.Abstractions;
using Domain.Entities;
using Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    /// <summary>
    /// Serviço responsável pelo rebalanceamento de carteiras de clientes.
    /// Implementa dois cenários:
    /// A) Mudança na composição da cesta Top Five
    /// B) Rebalanceamento por desvio de proporção
    /// </summary>
    public interface IRebalanceamentoService
    {
        Task RebalancearPorMudancaCesta(int clienteId, CestaRecomendacao novaCesta);
        Task RebalancearPorDesvioDeProporção(int clienteId, decimal toleranciaDesvio = 0.05m);
    }

    public class RebalanceamentoService : IRebalanceamentoService
    {
        private readonly IAppDbContext _context;
        private readonly ICotahistParser _parser;
        private readonly IKafkaProducer _kafkaProducer;
        private readonly ILogger<RebalanceamentoService> _logger;
        private readonly string _pastaCotacoes;

        public RebalanceamentoService(
            IAppDbContext context,
            ICotahistParser parser,
            IKafkaProducer kafkaProducer,
            ILogger<RebalanceamentoService> logger,
            string pastaCotacoes)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _kafkaProducer = kafkaProducer ?? throw new ArgumentNullException(nameof(kafkaProducer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _pastaCotacoes = pastaCotacoes ?? throw new ArgumentNullException(nameof(pastaCotacoes));
        }

        /// <summary>
        /// Cenário A: Rebalanceamento por mudança na composição da cesta Top Five.
        /// 
        /// Processo:
        /// 1. Identificar ativos que saíram da cesta
        /// 2. Vender posição dos ativos que saíram
        /// 3. Calcular IR sobre vendas (se > R$ 20.000)
        /// 4. Comprar novos ativos segundo a nova composição
        /// </summary>
        public async Task RebalancearPorMudancaCesta(int clienteId, CestaRecomendacao novaCesta)
        {
            _logger.LogInformation("🔄 Iniciando rebalanceamento por mudança de cesta para cliente {ClienteId}", clienteId);

            var cliente = await _context.Clientes
                .Include(c => c.Custodias)
                .FirstOrDefaultAsync(c => c.Id == clienteId);

            if (cliente == null)
            {
                _logger.LogWarning("⚠️ Cliente {ClienteId} não encontrado", clienteId);
                return;
            }

            if (!cliente.Ativo)
            {
                _logger.LogWarning("⚠️ Cliente {ClienteId} inativo. Rebalanceamento cancelado", clienteId);
                return;
            }

            // Validar nova cesta
            if (novaCesta?.Itens == null || novaCesta.Itens.Count != 5)
            {
                throw new InvalidOperationException("Nova cesta deve conter exatamente 5 ativos");
            }

            var tickersNovos = novaCesta.Itens.Select(i => i.Ticker).ToList();
            var tickersAntigos = cliente.Custodias.Select(c => c.Ticker).Distinct().ToList();
            var tickersParaVender = tickersAntigos.Except(tickersNovos).ToList();

            _logger.LogInformation("📊 Ativos a vender: {TickersParaVender}", string.Join(", ", tickersParaVender));

            decimal valorTotalVendasMes = 0;
            decimal lucroTotalVendasMes = 0;
            var custodiasVendidas = new List<CustodiaFilhote>();

            // ETAPA 1: Vender ativos que saíram da cesta
            foreach (var ticker in tickersParaVender)
            {
                var custodia = cliente.Custodias.FirstOrDefault(c => c.Ticker == ticker);
                if (custodia == null)
                    continue;

                var cotacao = _parser.ObterCotacaoFechamento(_pastaCotacoes, ticker);
                if (cotacao == null)
                {
                    _logger.LogWarning("⚠️ Cotação não encontrada para {Ticker}", ticker);
                    continue;
                }

                decimal valorVenda = custodia.Quantidade * cotacao.PrecoFechamento;
                decimal lucro = valorVenda - (custodia.Quantidade * custodia.PrecoMedio);

                valorTotalVendasMes += valorVenda;
                lucroTotalVendasMes += lucro;

                _logger.LogInformation(
                    "💰 Venda de {Ticker}: {Quantidade} ações @ R$ {Preco} = R$ {Valor}",
                    ticker,
                    custodia.Quantidade,
                    cotacao.PrecoFechamento,
                    valorVenda);

                // Registrar venda (custódia + ledger mensal)
                int qtdVendida = custodia.Quantidade;
                custodia.RegistrarVenda(qtdVendida);
                custodiasVendidas.Add(custodia);
                RegistrarMovimentacaoVenda(cliente.Id, ticker, qtdVendida, valorVenda, lucro);

                // IR Dedo-duro (0,005% sobre venda)
                await RegistrarIRDedoDuro(cliente.Id, cliente.CPF, ticker, valorVenda);
            }

            // ETAPA 2: IR sobre lucro considerando o total de vendas do mês
            await ApurarIRMensalAsync(cliente, valorTotalVendasMes, lucroTotalVendasMes);

            // ETAPA 3: Remover custódias vendidas
            foreach (var custodia in custodiasVendidas)
            {
                _context.CustodiasFilhotes.Remove(custodia);
            }

            // ETAPA 4: Comprar novos ativos
            decimal valorDisponivel = valorTotalVendasMes;
            foreach (var item in novaCesta.Itens)
            {
                if (tickersNovos.Contains(item.Ticker) && !tickersAntigos.Contains(item.Ticker))
                {
                    var cotacao = _parser.ObterCotacaoFechamento(_pastaCotacoes, item.Ticker);
                    if (cotacao == null)
                        continue;

                    decimal valorCompra = valorDisponivel * (item.Percentual / 100);
                    int quantidade = (int)(valorCompra / cotacao.PrecoFechamento);

                    if (quantidade > 0)
                    {
                        var novaCustodia = new CustodiaFilhote
                        {
                            ClienteId = cliente.Id,
                            Ticker = item.Ticker,
                            Quantidade = quantidade,
                            PrecoMedio = cotacao.PrecoFechamento,
                            DataAquisicao = DateTime.UtcNow
                        };

                        _context.CustodiasFilhotes.Add(novaCustodia);

                        _logger.LogInformation(
                            "📈 Compra de {Ticker}: {Quantidade} ações @ R$ {Preco}",
                            item.Ticker,
                            quantidade,
                            cotacao.PrecoFechamento);

                        // IR Dedo-duro sobre compra
                        await RegistrarIRDedoDuro(cliente.Id, cliente.CPF, item.Ticker, quantidade * cotacao.PrecoFechamento);
                    }
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("✅ Rebalanceamento por mudança de cesta concluído para cliente {ClienteId}", clienteId);
        }

        /// <summary>
        /// Cenário B: Rebalanceamento por desvio de proporção.
        /// 
        /// Processo:
        /// 1. Calcular proporção atual de cada ativo na carteira
        /// 2. Comparar com percentuais da cesta Top Five
        /// 3. Vender ativos acima da proporção alvo
        /// 4. Comprar ativos abaixo da proporção alvo
        /// 5. Calcular IR conforme regras
        /// </summary>
        public async Task RebalancearPorDesvioDeProporção(int clienteId, decimal toleranciaDesvio = 0.05m)
        {
            _logger.LogInformation(
                "🔄 Iniciando rebalanceamento por desvio de proporção para cliente {ClienteId}",
                clienteId);

            var cliente = await _context.Clientes
                .Include(c => c.Custodias)
                .FirstOrDefaultAsync(c => c.Id == clienteId);

            if (cliente == null)
            {
                _logger.LogWarning("⚠️ Cliente {ClienteId} não encontrado", clienteId);
                return;
            }

            if (!cliente.Ativo)
            {
                _logger.LogWarning("⚠️ Cliente {ClienteId} inativo. Rebalanceamento cancelado", clienteId);
                return;
            }

            var cestaAtiva = await _context.CestasRecomendacao
                .Include(c => c.Itens)
                .FirstOrDefaultAsync(c => c.Ativa);

            if (cestaAtiva == null)
            {
                _logger.LogWarning("⚠️ Nenhuma cesta ativa encontrada");
                return;
            }

            // ETAPA 1: Calcular valor total da carteira
            decimal valorTotalCarteira = 0;
            var cotacoes = new Dictionary<string, CotacaoB3>();

            foreach (var custodia in cliente.Custodias)
            {
                var cotacao = _parser.ObterCotacaoFechamento(_pastaCotacoes, custodia.Ticker);
                if (cotacao != null)
                {
                    cotacoes[custodia.Ticker] = cotacao;
                    valorTotalCarteira += custodia.Quantidade * cotacao.PrecoFechamento;
                }
            }

            if (valorTotalCarteira == 0)
            {
                _logger.LogWarning("⚠️ Carteira vazia. Nenhum rebalanceamento necessário");
                return;
            }

            decimal valorTotalVendasMes = 0;
            decimal lucroTotalVendasMes = 0;

            // ETAPA 2: Analisar desvios e executar rebalanceamento
            foreach (var item in cestaAtiva.Itens)
            {
                var custodia = cliente.Custodias.FirstOrDefault(c => c.Ticker == item.Ticker);
                
                if (custodia == null)
                    continue;

                if (!cotacoes.TryGetValue(item.Ticker, out var cotacao))
                    continue;

                decimal valorAtualAtivo = custodia.Quantidade * cotacao.PrecoFechamento;
                decimal proporcaoAtual = valorAtualAtivo / valorTotalCarteira;
                decimal proporcaoAlvo = item.Percentual / 100;
                decimal desvio = Math.Abs(proporcaoAtual - proporcaoAlvo);

                _logger.LogInformation(
                    "📊 {Ticker}: Proporção atual {ProporcaoAtual:P2}, Alvo {ProporcaoAlvo:P2}, Desvio {Desvio:P2}",
                    item.Ticker,
                    proporcaoAtual,
                    proporcaoAlvo,
                    desvio);

                // Se desvio > tolerância, rebalancear
                if (desvio > toleranciaDesvio)
                {
                    if (proporcaoAtual > proporcaoAlvo)
                    {
                        // VENDER: Ativo acima da proporção
                        int quantidadeVender = (int)((proporcaoAtual - proporcaoAlvo) * valorTotalCarteira / cotacao.PrecoFechamento);
                        
                        if (quantidadeVender > 0 && quantidadeVender <= custodia.Quantidade)
                        {
                            decimal valorVenda = quantidadeVender * cotacao.PrecoFechamento;
                            decimal lucro = valorVenda - (quantidadeVender * custodia.PrecoMedio);

                            valorTotalVendasMes += valorVenda;
                            lucroTotalVendasMes += lucro;

                            _logger.LogInformation(
                                "💰 Venda de {Ticker}: {Quantidade} ações @ R$ {Preco}",
                                item.Ticker,
                                quantidadeVender,
                                cotacao.PrecoFechamento);

                            custodia.RegistrarVenda(quantidadeVender);
                            RegistrarMovimentacaoVenda(cliente.Id, item.Ticker, quantidadeVender, valorVenda, lucro);
                            await RegistrarIRDedoDuro(cliente.Id, cliente.CPF, item.Ticker, valorVenda);
                        }
                    }
                    else if (proporcaoAtual < proporcaoAlvo)
                    {
                        // COMPRAR: Ativo abaixo da proporção
                        decimal valorCompra = (proporcaoAlvo - proporcaoAtual) * valorTotalCarteira;
                        int quantidadeComprar = (int)(valorCompra / cotacao.PrecoFechamento);

                        if (quantidadeComprar > 0)
                        {
                            _logger.LogInformation(
                                "📈 Compra de {Ticker}: {Quantidade} ações @ R$ {Preco}",
                                item.Ticker,
                                quantidadeComprar,
                                cotacao.PrecoFechamento);

                            custodia.AtualizarPrecoMedio(quantidadeComprar, cotacao.PrecoFechamento);
                            await RegistrarIRDedoDuro(cliente.Id, cliente.CPF, item.Ticker, valorCompra);
                        }
                    }
                }
            }

            // ETAPA 3: IR sobre lucro considerando o total de vendas do mês
            await ApurarIRMensalAsync(cliente, valorTotalVendasMes, lucroTotalVendasMes);

            await _context.SaveChangesAsync();
            _logger.LogInformation(
                "✅ Rebalanceamento por desvio de proporção concluído para cliente {ClienteId}",
                clienteId);
        }

        /// <summary>
        /// Registra evento de IR Dedo-Duro (0,005% sobre operação).
        /// </summary>
        /// <summary>
        /// Registra a venda no ledger mensal (base para a regra de isenção de R$ 20.000).
        /// </summary>
        private void RegistrarMovimentacaoVenda(int clienteId, string ticker, int quantidade, decimal valorVenda, decimal lucro)
        {
            var agora = DateTime.UtcNow;
            _context.MovimentacoesVenda.Add(new MovimentacaoVenda
            {
                ClienteId = clienteId,
                Ticker = ticker,
                Quantidade = quantidade,
                ValorVenda = valorVenda,
                Lucro = lucro,
                Data = agora,
                Ano = agora.Year,
                Mes = agora.Month
            });
        }

        /// <summary>
        /// Aplica a regra de IR sobre lucro considerando o total de vendas do MÊS
        /// (vendas já persistidas + as desta operação). Acima de R$ 20.000, 20% do lucro.
        /// </summary>
        private async Task ApurarIRMensalAsync(Cliente cliente, decimal vendasDestaOperacao, decimal lucroDestaOperacao)
        {
            if (lucroDestaOperacao <= 0)
                return;

            var agora = DateTime.UtcNow;
            var vendasPersistidasMes = await _context.MovimentacoesVenda
                .Where(v => v.ClienteId == cliente.Id && v.Ano == agora.Year && v.Mes == agora.Month)
                .SumAsync(v => v.ValorVenda);

            var totalMes = vendasPersistidasMes + vendasDestaOperacao;
            var ir = CalculadoraIR.IrSobreLucroMensal(totalMes, lucroDestaOperacao);
            if (ir > 0)
                await RegistrarIRSobreLucroMensal(cliente.Id, cliente.CPF, lucroDestaOperacao, totalMes);
        }

        private async Task RegistrarIRDedoDuro(int clienteId, string cpf, string ticker, decimal valorOperacao)
        {
            decimal valorIR = CalculadoraIR.DedoDuro(valorOperacao);

            var evento = new EventoIR
            {
                ClienteId = clienteId,
                CPF = cpf, // RN-056
                Tipo = TipoIR.DEDO_DURO,
                ValorBase = valorOperacao,
                Aliquota = CalculadoraIR.AliquotaDedoDuro,
                ValorImposto = valorIR,
                DataEvento = DateTime.UtcNow,
                Ticker = ticker,
                Descricao = $"IR Dedo-Duro sobre operação com {ticker}",
                PublicadoKafka = false
            };

            _context.EventosIR.Add(evento);
            await _kafkaProducer.PublicarEventoIR(evento);

            _logger.LogDebug("💳 IR Dedo-Duro registrado: R$ {ValorIR} sobre R$ {ValorOperacao}", valorIR, valorOperacao);
        }

        /// <summary>
        /// Registra evento de IR sobre Lucro Mensal (20% sobre lucro quando vendas > R$ 20.000).
        /// </summary>
        private async Task RegistrarIRSobreLucroMensal(int clienteId, string cpf, decimal lucroTotal, decimal valorVendasTotal)
        {
            decimal valorIR = lucroTotal * CalculadoraIR.AliquotaLucro; // 20%

            var evento = new EventoIR
            {
                ClienteId = clienteId,
                CPF = cpf, // RN-056
                Tipo = TipoIR.LUCRO_MENSAL,
                ValorBase = lucroTotal,
                Aliquota = CalculadoraIR.AliquotaLucro,
                ValorImposto = valorIR,
                DataEvento = DateTime.UtcNow,
                Descricao = $"IR 20% sobre lucro mensal (vendas: R$ {valorVendasTotal:F2})",
                PublicadoKafka = false
            };

            _context.EventosIR.Add(evento);
            await _kafkaProducer.PublicarEventoIR(evento);

            _logger.LogInformation(
                "💳 IR sobre Lucro Mensal registrado: R$ {ValorIR} (20% de R$ {Lucro})",
                valorIR,
                lucroTotal);
        }
    }
}
