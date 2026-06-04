using Application.Abstractions;
using Domain.Entities;
using Domain.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    /// <summary>
    /// Serviço responsável pela execução do motor de compra programada.
    /// Implementa a lógica completa de compra, distribuição e cálculo de IR.
    /// Segue padrão Clean Architecture com Dependency Injection.
    /// </summary>
    public class MotorCompraService : IMotorCompraService
    {
        private readonly IAppDbContext _context;
        private readonly ICotahistParser _parser;
        private readonly IKafkaProducer _kafkaProducer;
        private readonly string _pastaCotacoes;

        /// <summary>
        /// Construtor com injeção de dependências.
        /// </summary>
        public MotorCompraService(
            IAppDbContext context,
            ICotahistParser parser,
            IKafkaProducer kafkaProducer,
            string pastaCotacoes)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _kafkaProducer = kafkaProducer ?? throw new ArgumentNullException(nameof(kafkaProducer));
            _pastaCotacoes = pastaCotacoes ?? throw new ArgumentNullException(nameof(pastaCotacoes));
        }

        /// <summary>
        /// Valida se a data de execução é um dia de compra válido.
        /// RN-020: compras nos dias 5, 15 e 25.
        /// RN-021/RN-022: se o dia 5/15/25 cair em sábado/domingo, a execução
        /// ocorre no próximo dia útil (segunda a sexta).
        /// </summary>
        private void ValidarDiaCompra(DateTime data)
        {
            if (!CalendarioPregao.EhDataDeExecucaoValida(data))
            {
                throw new InvalidOperationException(
                    $"Execução de compra programada permitida apenas nos dias 5, 15, 25 " +
                    $"(ou no próximo dia útil, caso caiam em fim de semana). " +
                    $"Data informada: {data:yyyy-MM-dd} ({data.DayOfWeek}).");
            }
        }

        /// <summary>
        /// Executa a compra programada para uma data específica.
        /// 
        /// Fluxo de 8 etapas:
        /// 1. Validar se é dia de compra (5, 15, 25 ou próximo dia útil)
        /// 2. Obter clientes ativos e cesta vigente
        /// 3. Agrupamento de pedidos (1/3 do valor mensal)
        /// 4. Calcular compra consolidada por ativo
        /// 5. Separar em lote padrão (100) e fracionário (sufixo F)
        /// 6. Executar compra (registro de ordem)
        /// 7. Distribuição para contas filhotes
        /// 8. Atualizar resíduos na custódia Master
        /// </summary>
        public async Task ExecutarCompraProgramada(DateTime dataReferencia)
        {
            // 1. Validar se é dia de compra (5, 15, 25 ou próximo dia útil)
            ValidarDiaCompra(dataReferencia);

            // 1.1 Idempotência: impede reexecução para a mesma data (evita duplicar
            // ordens, custódias, histórico de aporte e eventos de IR).
            bool jaExecutado = await _context.HistoricoAportes
                .AnyAsync(h => h.Data.Date == dataReferencia.Date);
            if (jaExecutado)
            {
                throw new InvalidOperationException(
                    $"Compra programada já executada para a data {dataReferencia:yyyy-MM-dd}.");
            }

            // Eventos de IR a publicar no Kafka somente após persistência (evita dual-write
            // inconsistente: nada vai ao tópico se o SaveChanges falhar).
            var eventosParaPublicar = new List<EventoIR>();

            // 2. Obter clientes ativos e cesta vigente
            var clientesAtivos = await _context.Clientes
                .Where(c => c.Ativo)
                .ToListAsync();

            var cestaVigente = await _context.CestasRecomendacao
                .Include(c => c.Itens)
                .FirstOrDefaultAsync(c => c.Ativa);

            if (cestaVigente == null || !clientesAtivos.Any())
                return;

            // 3. Agrupamento de pedidos (1/3 do valor mensal)
            decimal totalAportes = clientesAtivos.Sum(c => c.ValorMensalAporte / 3);

            // 4. Calcular compra consolidada por ativo
            foreach (var itemCesta in cestaVigente.Itens)
            {
                decimal valorAlocado = totalAportes * (itemCesta.Percentual / 100);
                var cotacao = _parser.ObterCotacaoFechamento(_pastaCotacoes, itemCesta.Ticker);

                if (cotacao == null)
                    continue;

                int quantidadeNecessaria = (int)Math.Truncate(valorAlocado / cotacao.PrecoFechamento);

                // 5. Considerar saldo da custódia master
                var saldoMaster = await _context.CustodiasMaster
                    .FirstOrDefaultAsync(m => m.Ticker == itemCesta.Ticker);

                int quantidadeSaldoMaster = saldoMaster?.Quantidade ?? 0;
                int quantidadeAComprar = Math.Max(0, quantidadeNecessaria - quantidadeSaldoMaster);

                // 6. Executar compra (Simulação de registro de ordem)
                if (quantidadeAComprar > 0)
                {
                    var ordem = new OrdemCompra
                    {
                        Ticker = itemCesta.Ticker,
                        QuantidadeTotal = quantidadeAComprar,
                        PrecoUnitario = cotacao.PrecoFechamento,
                        ValorTotal = quantidadeAComprar * cotacao.PrecoFechamento,
                        Tipo = TipoOrdem.COMPRA,
                        DataExecucao = DateTime.UtcNow,
                        Referencia = $"Compra Programada {dataReferencia:yyyy-MM-dd}"
                    };

                    // Separar Lote Padrão vs Fracionário
                    int lotesPadrao = (quantidadeAComprar / 100) * 100;
                    int fracionario = quantidadeAComprar % 100;

                    if (lotesPadrao > 0)
                        ordem.Detalhes.Add(new DetalheExecucaoOrdem
                        {
                            Mercado = Mercado.PADRAO,
                            TickerExecutado = itemCesta.Ticker,
                            Quantidade = lotesPadrao
                        });

                    if (fracionario > 0)
                        ordem.Detalhes.Add(new DetalheExecucaoOrdem
                        {
                            Mercado = Mercado.FRACIONARIO,
                            TickerExecutado = itemCesta.Ticker + "F",
                            Quantidade = fracionario
                        });

                    _context.OrdensCompra.Add(ordem);
                }

                // 7. Distribuição para contas filhotes
                int totalDisponivel = quantidadeAComprar + quantidadeSaldoMaster;
                int totalDistribuido = 0;

                foreach (var cliente in clientesAtivos)
                {
                    decimal proporcaoCliente = (cliente.ValorMensalAporte / 3) / totalAportes;
                    int qtdCliente = (int)Math.Truncate(proporcaoCliente * totalDisponivel);

                    if (qtdCliente > 0)
                    {
                        var custodia = await _context.CustodiasFilhotes
                            .FirstOrDefaultAsync(cf => cf.ClienteId == cliente.Id && cf.Ticker == itemCesta.Ticker);

                        if (custodia == null)
                        {
                            custodia = new CustodiaFilhote
                            {
                                ClienteId = cliente.Id,
                                Ticker = itemCesta.Ticker,
                                Quantidade = 0,
                                PrecoMedio = 0
                            };
                            _context.CustodiasFilhotes.Add(custodia);
                        }

                        custodia.AtualizarPrecoMedio(qtdCliente, cotacao.PrecoFechamento);
                        totalDistribuido += qtdCliente;

                        // Registrar histórico de aporte
                        _context.HistoricoAportes.Add(new HistoricoAporte
                        {
                            ClienteId = cliente.Id,
                            Data = dataReferencia,
                            Valor = cliente.ValorMensalAporte / 3,
                            Parcela = GetParcela(dataReferencia)
                        });

                        // IR Dedo-duro (0,005% sobre valor da operação)
                        // Regra: "a cada operação de compra distribuída"
                        var eventoIR = new EventoIR
                        {
                            ClienteId = cliente.Id,
                            CPF = cliente.CPF, // RN-056
                            Tipo = TipoIR.DEDO_DURO,
                            ValorBase = qtdCliente * cotacao.PrecoFechamento,
                            Aliquota = CalculadoraIR.AliquotaDedoDuro,
                            ValorImposto = CalculadoraIR.DedoDuro(qtdCliente * cotacao.PrecoFechamento),
                            DataEvento = DateTime.UtcNow,
                            Ticker = itemCesta.Ticker,
                            Descricao = "IR Dedo-duro sobre compra programada"
                        };
                        _context.EventosIR.Add(eventoIR);
                        eventosParaPublicar.Add(eventoIR);
                    }
                }

                // 8. Atualizar Resíduos na Master
                int residuo = totalDisponivel - totalDistribuido;
                if (saldoMaster == null)
                {
                    saldoMaster = new CustodiaMaster
                    {
                        Ticker = itemCesta.Ticker,
                        Quantidade = residuo,
                        PrecoMedio = cotacao.PrecoFechamento,
                        Origem = $"Residuo {dataReferencia:yyyy-MM-dd}"
                    };
                    _context.CustodiasMaster.Add(saldoMaster);
                }
                else
                {
                    saldoMaster.Quantidade = residuo;
                    saldoMaster.PrecoMedio = cotacao.PrecoFechamento;
                    saldoMaster.Origem = $"Residuo {dataReferencia:yyyy-MM-dd}";
                }
            }

            await _context.SaveChangesAsync();

            // Publica no Kafka somente após o commit (RN-055): garante que o tópico
            // reflita apenas eventos efetivamente persistidos.
            foreach (var evento in eventosParaPublicar)
            {
                await _kafkaProducer.PublicarEventoIR(evento);
            }
        }

        /// <summary>
        /// Obtém o status da última execução do motor de compra.
        /// </summary>
        public async Task<StatusMotorCompraDto?> ObterStatusUltimaExecucao()
        {
            var ultimaOrdem = await _context.OrdensCompra
                .OrderByDescending(o => o.DataExecucao)
                .FirstOrDefaultAsync();

            if (ultimaOrdem == null)
                return null;

            var eventosIR = await _context.EventosIR
                .Where(e => e.DataEvento.Date == ultimaOrdem.DataExecucao.Date)
                .ToListAsync();

            return new StatusMotorCompraDto
            {
                DataExecucao = ultimaOrdem.DataExecucao,
                ClientesProcessados = await _context.Clientes.CountAsync(c => c.Ativo),
                ValorTotalProcessado = eventosIR.Sum(e => e.ValorBase),
                OrdensCriadas = await _context.OrdensCompra.CountAsync(),
                TotalIR = eventosIR.Sum(e => e.ValorImposto),
                Sucesso = true,
                Mensagem = "Última execução concluída com sucesso"
            };
        }

        /// <summary>
        /// Determina a parcela de aporte baseado no dia do mês.
        /// </summary>
        private string GetParcela(DateTime data)
        {
            if (data.Day <= 10)
                return "1/3";
            if (data.Day <= 20)
                return "2/3";
            return "3/3";
        }
    }
}
