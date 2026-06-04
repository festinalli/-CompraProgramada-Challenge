namespace Domain.Constants
{
    /// <summary>
    /// Constantes centralizadas para regras de negócio.
    /// Evita magic numbers e facilita manutenção.
    /// </summary>
    public static class ConstantesNegocio
    {
        // Limites de Aporte
        public const decimal VALOR_MINIMO_APORTE = 100m;
        public const decimal VALOR_MAXIMO_APORTE = 1_000_000m;

        // Cesta de Recomendação
        public const int QUANTIDADE_ATIVOS_CESTA = 5;
        public const decimal PERCENTUAL_MINIMO_ATIVO = 0.01m; // 1%
        public const decimal PERCENTUAL_MAXIMO_ATIVO = 0.99m; // 99%
        public const decimal SOMA_PERCENTUAIS_ESPERADA = 1.0m; // 100%
        public const decimal TOLERANCIA_PERCENTUAL = 0.001m; // 0.1%

        // Lotes de Compra
        public const int QUANTIDADE_LOTE_PADRAO = 100;
        public const char SUFIXO_LOTE_FRACIONARIO = 'F';

        // Datas de Execução
        public static readonly int[] DIAS_EXECUCAO = { 5, 15, 25 };

        // IR (Imposto de Renda)
        public const decimal ALIQUOTA_IR_DEDO_DURO = 0.00005m; // 0,005%
        public const decimal ALIQUOTA_IR_LUCRO = 0.20m; // 20%
        public const decimal LIMITE_ISENCAO_IR_LUCRO = 20_000m; // R$ 20.000

        // Rebalanceamento
        public const decimal TOLERANCIA_DESVIO_REBALANCEAMENTO = 0.05m; // 5%
        public const int DIAS_REBALANCEAMENTO_MINIMO = 30; // A cada 30 dias

        // Divisão de Aportes
        public const int QUANTIDADE_PARCELAS_APORTE = 3;

        // Validação de CPF
        public const int TAMANHO_CPF = 11;

        // Resíduos
        public const decimal LIMITE_RESIDUO_MINIMO = 0.01m; // R$ 0,01
    }
}
