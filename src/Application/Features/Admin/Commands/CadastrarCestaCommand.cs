using MediatR;
using System.ComponentModel.DataAnnotations;

namespace Application.Features.Admin.Commands
{
    /// <summary>
    /// Command para cadastrar cesta recomendada (Top Five).
    /// Implementa validações robustas:
    /// - Exatamente 5 ativos
    /// - Percentuais somam 100% (com tolerância de ponto flutuante 0.01%)
    /// - Tickers válidos e únicos
    /// - Transações explícitas para atomicidade
    /// - Rebalanceamento automático se houver cesta anterior
    /// Segue padrão CQRS - operação de escrita com autorização Admin.
    /// </summary>
    public class CadastrarCestaCommand : IRequest<CadastrarCestaResponse>
    {
        [Required(ErrorMessage = "Nome da cesta é obrigatório")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Nome deve ter entre 3 e 100 caracteres")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ativos são obrigatórios")]
        [MinLength(5, ErrorMessage = "A cesta deve conter exatamente 5 ativos")]
        [MaxLength(5, ErrorMessage = "A cesta deve conter exatamente 5 ativos")]
        public List<ItemCestaRequest> Ativos { get; set; } = new();
    }

    public class ItemCestaRequest
    {
        [Required(ErrorMessage = "Ticker é obrigatório")]
        [RegularExpression(@"^[A-Z]{4}\d{1}$|^[A-Z]{4}$", ErrorMessage = "Ticker inválido (ex: PETR4, VALE3)")]
        public string Ticker { get; set; } = string.Empty;

        [Required(ErrorMessage = "Percentual é obrigatório")]
        [Range(0.01, 100, ErrorMessage = "Percentual deve estar entre 0.01 e 100")]
        public decimal Percentual { get; set; }
    }

    public class CadastrarCestaResponse
    {
        public int CestaId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public List<ItemCestaResponse> Ativos { get; set; } = new();
        public DateTime DataCriacao { get; set; }
        public bool RebalanceamentoDisparado { get; set; }
        public string Mensagem { get; set; } = string.Empty;
    }

    public class ItemCestaResponse
    {
        public string Ticker { get; set; } = string.Empty;
        public decimal Percentual { get; set; }
    }
}
