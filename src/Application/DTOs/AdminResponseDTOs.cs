namespace Application.DTOs
{
    public class CestaResponse
    {
        public int CestaId { get; set; }
        public DateTime DataCadastro { get; set; }
        public DateTime DataCriacao { get; set; }
        public bool Ativa { get; set; }
        public string Mensagem { get; set; } = string.Empty;
        public List<CestaAtivoResponse> Ativos { get; set; } = new();
    }

    public class CestaAtivoResponse
    {
        public string Ticker { get; set; } = string.Empty;
        public decimal Percentual { get; set; }
    }

    public class MotorCompraResponse
    {
        public bool Sucesso { get; set; }
        public string Mensagem { get; set; } = string.Empty;
        public int ClientesProcessados { get; set; }
        public decimal ValorTotalComprado { get; set; }
        public decimal ValorTotalIR { get; set; }
        public DateTime DataExecucao { get; set; }
        public DateTime DataReferencia { get; set; }
        public int TotalCompras { get; set; }
        public decimal ValorTotal { get; set; }
        public List<OrdemCompraResponse> Ordens { get; set; } = new();
    }

    public class OrdemCompraResponse
    {
        public int OrdemId { get; set; }
        public int ClienteId { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public int Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }
        public decimal ValorTotal { get; set; }
        public string TipoLote { get; set; } = string.Empty;
    }

    public class CadastrarCestaRequest
    {
        public string Nome { get; set; } = string.Empty;
        public List<CestaAtivoRequest> Ativos { get; set; } = new();
    }

    public class CestaAtivoRequest
    {
        public string Ticker { get; set; } = string.Empty;
        public decimal Percentual { get; set; }
    }

    public class ExecutarMotorRequest
    {
        public DateTime DataReferencia { get; set; }
    }

    public class CestaHistoricoResponse
    {
        public int CestaId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public bool Ativa { get; set; }
        public DateTime DataCriacao { get; set; }
        public DateTime? DataDesativacao { get; set; }
        public List<CestaAtivoResponse> Ativos { get; set; } = new();
    }

    public class CustodiaMasterResponse
    {
        public string Ticker { get; set; } = string.Empty;
        public int Quantidade { get; set; }
        public int QuantidadeLotePadrao { get; set; }
        public int QuantidadeFracionario { get; set; }
        public decimal PrecoMedio { get; set; }
        public decimal ValorAtual { get; set; }
    }
}