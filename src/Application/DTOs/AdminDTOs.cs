namespace Application.DTOs
{
    /// <summary>
    /// DTO para cadastro de cesta de recomendação.
    /// </summary>
    public class CadastroCestaRequest
    {
        public List<AtivoComPorcentagemRequest> Ativos { get; set; } = new();
    }

    /// <summary>
    /// DTO para ativo com percentual.
    /// </summary>
    public class AtivoComPorcentagemRequest
    {
        public string Ticker { get; set; } = string.Empty;
        public decimal Percentual { get; set; }
    }

    /// <summary>
    /// DTO para resposta de cadastro de cesta.
    /// </summary>
    public class CadastroCestaResponse
    {
        public int CestaId { get; set; }
        public DateTime DataCriacao { get; set; }
        public List<AtivoComPorcentagemResponse> Ativos { get; set; } = new();
        public string Mensagem { get; set; } = "Cesta cadastrada com sucesso";
    }

    /// <summary>
    /// DTO para ativo com percentual (resposta).
    /// </summary>
    public class AtivoComPorcentagemResponse
    {
        public string Ticker { get; set; } = string.Empty;
        public decimal Percentual { get; set; }
    }

    /// <summary>
    /// DTO para consulta de cesta.
    /// </summary>
    public class ConsultaCestaResponse
    {
        public int CestaId { get; set; }
        public DateTime DataCriacao { get; set; }
        public List<AtivoComPorcentagemResponse> Ativos { get; set; } = new();
        public bool Ativa { get; set; }
    }

    /// <summary>
    /// DTO para execução do motor de compra.
    /// </summary>
    public class ExecutarMotorCompraRequest
    {
        public DateTime DataReferencia { get; set; }
    }

    /// <summary>
    /// DTO para resposta de execução do motor.
    /// </summary>
    public class ExecutarMotorCompraResponse
    {
        public bool Sucesso { get; set; }
        public string Mensagem { get; set; } = string.Empty;
        public int ClientesProcessados { get; set; }
        public decimal ValorTotalProcessado { get; set; }
        public DateTime DataReferencia { get; set; }
        public int TotalCompras { get; set; }
        public decimal ValorTotal { get; set; }
        public DateTime DataExecucao { get; set; }
    }

    /// <summary>
    /// DTO para consulta de custódia Master.
    /// </summary>
    public class ConsultaCustodiaMasterResponse
    {
        public decimal SaldoTotal { get; set; }
        public List<PosicaoMasterResponse> Posicoes { get; set; } = new();
        public decimal ResidualTotal { get; set; }
    }

    /// <summary>
    /// DTO para posição na custódia Master.
    /// </summary>
    public class PosicaoMasterResponse
    {
        public string Ticker { get; set; } = string.Empty;
        public int QuantidadeLotePadrao { get; set; }
        public int QuantidadeFracionario { get; set; }
        public decimal PrecoMedio { get; set; }
        public decimal ValorTotal { get; set; }
    }
}
