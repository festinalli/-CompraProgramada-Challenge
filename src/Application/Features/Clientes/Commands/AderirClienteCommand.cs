using MediatR;
using System.ComponentModel.DataAnnotations;

namespace Application.Features.Clientes.Commands
{
    /// <summary>
    /// Command para adesão de novo cliente ao programa de compra programada.
    /// Implementa validações robustas de CPF, valor mínimo e duplicidade.
    /// Segue padrão CQRS - operação de escrita.
    /// </summary>
    public class AderirClienteCommand : IRequest<AderirClienteResponse>
    {
        [Required(ErrorMessage = "CPF é obrigatório")]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "CPF deve conter 11 dígitos")]
        public string CPF { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nome é obrigatório")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "Nome deve ter entre 3 e 150 caracteres")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email é obrigatório")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Valor mensal é obrigatório")]
        [Range(100, 1000000, ErrorMessage = "Valor mensal deve estar entre R$ 100 e R$ 1.000.000")]
        public decimal ValorMensal { get; set; }

        [Required(ErrorMessage = "Senha é obrigatória")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Senha deve ter entre 8 e 100 caracteres")]
        public string Senha { get; set; } = string.Empty;
    }

    public class AderirClienteResponse
    {
        public int ClienteId { get; set; }
        public string CPF { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public decimal ValorMensal { get; set; }
        public DateTime DataAdesao { get; set; }
        public string Mensagem { get; set; } = string.Empty;
    }
}
