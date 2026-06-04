using Application.Abstractions;
using MediatR;
using Domain.Constants;
using Domain.Entities;
using Application.Features.Admin.Commands;
using Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Application.Features.Admin.Handlers
{
    /// <summary>
    /// Handler para CadastrarCestaCommand.
    /// Implementa:
    /// - Validação de exatamente 5 ativos
    /// - Validação de percentuais (soma = 100% com tolerância 0.01%)
    /// - Validação de tickers únicos e formato válido
    /// - Normalização de dados (trim, uppercase)
    /// - Transações explícitas para atomicidade
    /// - Rebalanceamento automático se houver cesta anterior
    /// - Logging estruturado
    /// - Tratamento específico de exceções
    /// </summary>
    public class CadastrarCestaCommandHandler : IRequestHandler<CadastrarCestaCommand, CadastrarCestaResponse>
    {
        private readonly IAppDbContext _context;
        private readonly IRebalanceamentoService _rebalanceamentoService;
        private readonly ILogger<CadastrarCestaCommandHandler> _logger;

        // Constantes
        private const int QUANTIDADE_ATIVOS_OBRIGATORIA = 5;
        private const decimal PERCENTUAL_TOTAL_ESPERADO = 100m;
        private const decimal TOLERANCIA_PONTO_FLUTUANTE = 0.01m;

        public CadastrarCestaCommandHandler(
            IAppDbContext context,
            IRebalanceamentoService rebalanceamentoService,
            ILogger<CadastrarCestaCommandHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _rebalanceamentoService = rebalanceamentoService ?? throw new ArgumentNullException(nameof(rebalanceamentoService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<CadastrarCestaResponse> Handle(CadastrarCestaCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Validar entrada
                ValidarCesta(request);

                // 2. Normalizar dados
                var nomeNormalizado = request.Nome.Trim().ToUpperInvariant();
                var ativosNormalizados = request.Ativos
                    .Select(a => new { Ticker = a.Ticker.Trim().ToUpperInvariant(), a.Percentual })
                    .ToList();

                // 3. Iniciar transação explícita
                using (var transaction = await _context.Database.BeginTransactionAsync(cancellationToken))
                {
                    try
                    {
                        // 4. Desativar cesta anterior (se existir)
                        var cestaAnterior = await _context.CestasRecomendacao
                            .AsNoTracking()
                            .FirstOrDefaultAsync(c => c.Ativa, cancellationToken);

                        bool rebalanceamentoDisparado = false;

                        if (cestaAnterior != null)
                        {
                            cestaAnterior.Ativa = false;
                            cestaAnterior.DataDesativacao = DateTime.UtcNow;
                            _context.CestasRecomendacao.Update(cestaAnterior);
                            await _context.SaveChangesAsync(cancellationToken);

                            // Disparar rebalanceamento automático
                            rebalanceamentoDisparado = true;
                            _logger.LogInformation("🔄 Rebalanceamento disparado automaticamente");
                        }

                        // 5. Criar nova cesta
                        var novaCesta = new CestaRecomendacao
                        {
                            Nome = nomeNormalizado,
                            Ativa = true,
                            DataCriacao = DateTime.UtcNow,
                            Itens = ativosNormalizados
                                .Select(a => new ItemCesta
                                {
                                    Ticker = a.Ticker,
                                    Percentual = a.Percentual
                                })
                                .ToList()
                        };

                        // 6. Salvar nova cesta
                        _context.CestasRecomendacao.Add(novaCesta);
                        await _context.SaveChangesAsync(cancellationToken);

                        // 6.1 RN-019/RN-045: trocar a cesta dispara rebalanceamento real
                        // para cada cliente ativo (vende ativos que saíram, compra os novos
                        // e ajusta os que permaneceram com percentual diferente).
                        if (rebalanceamentoDisparado)
                        {
                            var clientesAtivos = await _context.Clientes
                                .Where(c => c.Ativo)
                                .Select(c => c.Id)
                                .ToListAsync(cancellationToken);

                            foreach (var clienteId in clientesAtivos)
                            {
                                // Vende ativos que saíram e compra os novos (RN-046/047/048).
                                await _rebalanceamentoService.RebalancearPorMudancaCesta(clienteId, novaCesta);
                                // Ajusta os ativos que permaneceram mas mudaram de percentual,
                                // alinhando à nova composição ativa (RN-049/052).
                                await _rebalanceamentoService.RebalancearPorDesvioDeProporção(clienteId);
                            }

                            _logger.LogInformation(
                                "🔄 Rebalanceamento executado para {Total} cliente(s) ativo(s)",
                                clientesAtivos.Count);
                        }

                        // 7. Confirmar transação
                        await transaction.CommitAsync(cancellationToken);

                        _logger.LogInformation("✅ Cesta cadastrada com sucesso. CestaId: {CestaId}, Rebalanceamento: {Rebalanceamento}", 
                            novaCesta.Id, rebalanceamentoDisparado);

                        // 8. Retornar resposta
                        return new CadastrarCestaResponse
                        {
                            CestaId = novaCesta.Id,
                            Nome = novaCesta.Nome,
                            Ativos = ativosNormalizados
                                .Select(a => new ItemCestaResponse
                                {
                                    Ticker = a.Ticker,
                                    Percentual = a.Percentual
                                })
                                .ToList(),
                            DataCriacao = novaCesta.DataCriacao,
                            RebalanceamentoDisparado = rebalanceamentoDisparado,
                            Mensagem = rebalanceamentoDisparado 
                                ? "Cesta cadastrada com sucesso. Rebalanceamento disparado." 
                                : "Primeira cesta cadastrada com sucesso."
                        };
                    }
                    catch (ArgumentException ex)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        _logger.LogWarning("⚠️ Erro de validação: {Mensagem}", ex.Message);
                        throw;
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        _logger.LogError(ex, "❌ Erro ao cadastrar cesta");
                        throw new InvalidOperationException("Erro ao cadastrar cesta", ex);
                    }
                }
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("⚠️ Validação falhou: {Mensagem}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro inesperado ao cadastrar cesta");
                throw new InvalidOperationException("Erro ao cadastrar cesta", ex);
            }
        }

        /// <summary>
        /// Valida todos os dados da cesta.
        /// Verifica:
        /// - Nome não vazio
        /// - Exatamente 5 ativos
        /// - Tickers válidos e únicos
        /// - Percentuais somam 100% (com tolerância)
        /// </summary>
        private void ValidarCesta(CadastrarCestaCommand request)
        {
            // 1. Validar nome
            if (string.IsNullOrWhiteSpace(request.Nome))
                throw new ArgumentException("Nome da cesta é obrigatório");

            if (request.Nome.Length < 3 || request.Nome.Length > 100)
                throw new ArgumentException("Nome deve ter entre 3 e 100 caracteres");

            // 2. Validar quantidade de ativos
            if (request.Ativos == null || request.Ativos.Count != QUANTIDADE_ATIVOS_OBRIGATORIA)
                throw new ArgumentException($"A cesta deve conter exatamente {QUANTIDADE_ATIVOS_OBRIGATORIA} ativos");

            // 3. Validar tickers
            var tickersUnicos = new HashSet<string>();
            foreach (var ativo in request.Ativos)
            {
                if (string.IsNullOrWhiteSpace(ativo.Ticker))
                    throw new ArgumentException("Todos os ativos devem possuir um ticker válido");

                var tickerNormalizado = ativo.Ticker.Trim().ToUpperInvariant();
                
                // Validar formato do ticker (ex: PETR4, VALE3)
                if (!Regex.IsMatch(tickerNormalizado, @"^[A-Z]{4}\d{1}$|^[A-Z]{4}$"))
                    throw new ArgumentException($"Ticker inválido: {tickerNormalizado}");

                // Validar duplicidade
                if (!tickersUnicos.Add(tickerNormalizado))
                    throw new ArgumentException($"Ticker duplicado: {tickerNormalizado}");
            }

            // 4. Validar percentuais
            decimal percentualSum = request.Ativos.Sum(a => a.Percentual);

            // Usar tolerância para ponto flutuante (evita erro de 33.33 + 33.33 + 33.34 = 99.99999...)
            if (Math.Abs(percentualSum - PERCENTUAL_TOTAL_ESPERADO) > TOLERANCIA_PONTO_FLUTUANTE)
                throw new ArgumentException(
                    $"A soma dos percentuais deve ser aproximadamente 100%. Soma atual: {percentualSum:F2}%");

            // 5. Validar intervalos de percentuais
            foreach (var ativo in request.Ativos)
            {
                if (ativo.Percentual <= 0 || ativo.Percentual > 100)
                    throw new ArgumentException($"Percentual deve estar entre 0.01 e 100. Valor: {ativo.Percentual}");
            }
        }
    }
}
