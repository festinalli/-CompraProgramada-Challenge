namespace Application.DTOs
{
    /// <summary>
    /// DTO para requisição de adesão de cliente.
    /// Contém apenas dados necessários, sem exposição de entidades.
    /// </summary>
    public class AdesaoClienteRequest
    {
        public string CPF { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal ValorMensal { get; set; }
        public decimal ValorMensalAporte { get; set; }
        public string Senha { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para resposta de adesão.
    /// Não expõe dados sensíveis (CPF/Email mascarados).
    /// </summary>
    public class AdesaoClienteResponse
    {
        public int ClienteId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string CPFMascarado { get; set; } = string.Empty; // Ex: "123.456.789-**"
        public decimal ValorMensalAporte { get; set; }
        public DateTime DataAdesao { get; set; }
        public string Mensagem { get; set; } = "Adesão realizada com sucesso";
    }

    /// <summary>
    /// DTO para consulta de carteira do cliente.
    /// Retorna informações consolidadas sem dados sensíveis.
    /// </summary>
    public class CarteiraClienteResponse
    {
        public int ClienteId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public decimal SaldoTotal { get; set; }
        public decimal ValorInvestido { get; set; }
        public decimal ValorAtual { get; set; }
        public decimal Rentabilidade { get; set; }
        public decimal PercentualRentabilidade { get; set; }
        public List<PosicaoAtivoResponse> Posicoes { get; set; } = new();
    }

    /// <summary>
    /// DTO para posição de ativo na carteira.
    /// </summary>
    public class PosicaoAtivoResponse
    {
        public string Ticker { get; set; } = string.Empty;
        public int Quantidade { get; set; }
        public decimal PrecoMedio { get; set; }
        public decimal CotacaoAtual { get; set; }
        public decimal ValorAtual { get; set; }
        public decimal Rentabilidade { get; set; }
        public decimal PercentualCarteira { get; set; }
    }

    /// <summary>
    /// DTO para alterar valor mensal de aporte.
    /// </summary>
    public class AlterarAporteRequest
    {
        public int ClienteId { get; set; }
        public decimal NovoValorMensalAporte { get; set; }
    }

    /// <summary>
    /// DTO para saída de cliente.
    /// </summary>
    public class SaidaClienteRequest
    {
        public int ClienteId { get; set; }
        public string Motivo { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para saída de cliente (resposta).
    /// </summary>
    public class SaidaClienteResponse
    {
        public int ClienteId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public decimal SaldoFinal { get; set; }
        public DateTime DataSaida { get; set; }
        public string Mensagem { get; set; } = "Saída processada com sucesso";
    }
}
