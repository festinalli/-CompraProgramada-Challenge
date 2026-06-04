namespace Application.Services
{
    /// <summary>
    /// Interface para o Motor de Compra Programada.
    /// Define contrato para execução de compras automáticas de ativos.
    /// Segue padrão de Dependency Injection e Clean Architecture.
    /// </summary>
    public interface IMotorCompraService
    {
        /// <summary>
        /// Executa a compra programada para uma data específica.
        /// 
        /// Fluxo:
        /// 1. Valida data de execução (5, 15, 25 ou próximo dia útil)
        /// 2. Obtém clientes ativos e cesta vigente
        /// 3. Agrupa pedidos em 3 parcelas mensais
        /// 4. Calcula compra consolidada por ativo
        /// 5. Separa em lote padrão (100) e fracionário (sufixo F)
        /// 6. Distribui proporcionalmente aos clientes
        /// 7. Calcula IR Dedo-Duro (0,005%)
        /// 8. Publica eventos no Kafka
        /// </summary>
        /// <param name="dataReferencia">Data de referência para execução</param>
        /// <returns>Task assíncrona</returns>
        Task ExecutarCompraProgramada(DateTime dataReferencia);

        /// <summary>
        /// Obtém o status da última execução do motor.
        /// </summary>
        /// <returns>Informações da última execução</returns>
        Task<StatusMotorCompraDto?> ObterStatusUltimaExecucao();
    }

    /// <summary>
    /// DTO para status de execução do motor de compra.
    /// </summary>
    public class StatusMotorCompraDto
    {
        public DateTime DataExecucao { get; set; }
        public int ClientesProcessados { get; set; }
        public decimal ValorTotalProcessado { get; set; }
        public int OrdensCriadas { get; set; }
        public decimal TotalIR { get; set; }
        public bool Sucesso { get; set; }
        public string? Mensagem { get; set; }
    }
}
