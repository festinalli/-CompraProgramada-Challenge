using Application.Abstractions;
using MediatR;
using Domain.Constants;
using Domain.Entities;
using Application.Features.Clientes.Commands;
using Application.Services.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Application.Features.Clientes.Handlers
{
    /// <summary>
    /// Handler para AderirClienteCommand.
    /// Implementa:
    /// - Validação de CPF com dígitos verificadores
    /// - Verificação de duplicidade
    /// - Transações explícitas para atomicidade
    /// - Criptografia de senha com SHA256
    /// - Logging estruturado de segurança
    /// - Mascaramento de dados sensíveis
    /// </summary>
    public class AderirClienteCommandHandler : IRequestHandler<AderirClienteCommand, AderirClienteResponse>
    {
        private readonly IAppDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<AderirClienteCommandHandler> _logger;

        public AderirClienteCommandHandler(
            IAppDbContext context,
            IPasswordHasher passwordHasher,
            ILogger<AderirClienteCommandHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<AderirClienteResponse> Handle(AderirClienteCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Validar entrada
                ValidarAdesao(request);

                // 2. Normalizar dados
                var cpfNormalizado = Regex.Replace(request.CPF, @"\D", "");
                var nomeNormalizado = request.Nome.Trim().ToUpperInvariant();
                var emailNormalizado = request.Email.Trim().ToLowerInvariant();

                // 3. Iniciar transação explícita
                using (var transaction = await _context.Database.BeginTransactionAsync(cancellationToken))
                {
                    try
                    {
                        // 4. Verificar CPF duplicado (com índice)
                        var clienteExistente = await _context.Clientes
                            .AsNoTracking()
                            .FirstOrDefaultAsync(c => c.CPF == cpfNormalizado, cancellationToken);

                        if (clienteExistente != null)
                        {
                            _logger.LogWarning("⚠️ Tentativa de adesão com CPF duplicado: {CPF}", MascararCPF(cpfNormalizado));
                            throw new InvalidOperationException("CPF já cadastrado no sistema");
                        }

                        // 5. Criar novo cliente
                        var novoCliente = new Cliente
                        {
                            CPF = cpfNormalizado,
                            Nome = nomeNormalizado,
                            Email = emailNormalizado,
                            ValorMensalAporte = request.ValorMensal,
                            SenhaHash = _passwordHasher.Hash(request.Senha),
                            DataAdesao = DateTime.UtcNow,
                            Ativo = true
                        };

                        // 6. Salvar cliente primeiro para gerar o ID
                        _context.Clientes.Add(novoCliente);
                        await _context.SaveChangesAsync(cancellationToken);

                        // 7. Agora criar conta gráfica com o ID correto
                        var contaGrafica = new ContaGrafica
                        {
                            ClienteId = novoCliente.Id,
                            DataAbertura = DateTime.UtcNow,
                            Ativa = true
                        };

                        _context.ContasGraficas.Add(contaGrafica);
                        await _context.SaveChangesAsync(cancellationToken);

                        // 8. Confirmar transação
                        await transaction.CommitAsync(cancellationToken);

                        _logger.LogInformation("✅ Cliente aderido com sucesso. ClienteId: {ClienteId}", novoCliente.Id);

                        // 9. Retornar resposta com dados mascarados
                        return new AderirClienteResponse
                        {
                            ClienteId = novoCliente.Id,
                            CPF = MascararCPF(novoCliente.CPF),
                            Nome = novoCliente.Nome,
                            ValorMensal = novoCliente.ValorMensalAporte,
                            DataAdesao = novoCliente.DataAdesao,
                            Mensagem = "Adesão realizada com sucesso"
                        };
                    }
                    catch (InvalidOperationException ex)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        _logger.LogWarning("⚠️ Operação inválida: {Mensagem}", ex.Message);
                        throw;
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
                        _logger.LogError(ex, "❌ Erro inesperado ao aderir cliente");
                        throw new InvalidOperationException("Erro ao processar adesão", ex);
                    }
                }
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("⚠️ Validação falhou: {Mensagem}", ex.Message);
                throw;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("⚠️ Operação inválida: {Mensagem}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro inesperado ao processar adesão");
                throw new InvalidOperationException("Erro ao processar adesão", ex);
            }
        }

        /// <summary>
        /// Valida todos os dados de entrada para adesão.
        /// </summary>
        private void ValidarAdesao(AderirClienteCommand request)
        {
            if (string.IsNullOrWhiteSpace(request.CPF))
                throw new ArgumentException("CPF é obrigatório");

            if (!ValidarCPF(request.CPF))
                throw new ArgumentException("CPF inválido");

            if (string.IsNullOrWhiteSpace(request.Nome) || request.Nome.Length < 3)
                throw new ArgumentException("Nome deve ter no mínimo 3 caracteres");

            if (string.IsNullOrWhiteSpace(request.Email) || !ValidarEmail(request.Email))
                throw new ArgumentException("Email inválido");

            if (request.ValorMensal < ConstantesNegocio.VALOR_MINIMO_APORTE)
                throw new ArgumentException($"Valor mínimo de aporte é R$ {ConstantesNegocio.VALOR_MINIMO_APORTE}");

            if (request.ValorMensal > ConstantesNegocio.VALOR_MAXIMO_APORTE)
                throw new ArgumentException($"Valor máximo de aporte é R$ {ConstantesNegocio.VALOR_MAXIMO_APORTE}");

            if (string.IsNullOrWhiteSpace(request.Senha) || request.Senha.Length < 8)
                throw new ArgumentException("Senha deve ter no mínimo 8 caracteres");
        }

        /// <summary>
        /// Valida CPF com dígitos verificadores (módulo 11).
        /// </summary>
        private bool ValidarCPF(string cpf)
        {
            cpf = Regex.Replace(cpf, @"\D", "");

            if (cpf.Length != 11)
                return false;

            // CPF com todos os dígitos iguais é inválido
            if (cpf.Distinct().Count() == 1)
                return false;

            // Validar primeiro dígito verificador
            int soma = 0;
            for (int i = 0; i < 9; i++)
                soma += int.Parse(cpf[i].ToString()) * (10 - i);

            int resto = soma % 11;
            int digito1 = resto < 2 ? 0 : 11 - resto;

            if (cpf[9] != (char)('0' + digito1))
                return false;

            // Validar segundo dígito verificador
            soma = 0;
            for (int i = 0; i < 10; i++)
                soma += int.Parse(cpf[i].ToString()) * (11 - i);

            resto = soma % 11;
            int digito2 = resto < 2 ? 0 : 11 - resto;

            return cpf[10] == (char)('0' + digito2);
        }

        /// <summary>
        /// Valida formato de email.
        /// </summary>
        private bool ValidarEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Mascara CPF para exibição (XXX.XXX.XXX-XX).
        /// </summary>
        private string MascararCPF(string cpf)
        {
            cpf = Regex.Replace(cpf, @"\D", "");
            if (cpf.Length != 11) return "***.***.***-**";
            return $"{cpf.Substring(0, 3)}.{cpf.Substring(3, 3)}.{cpf.Substring(6, 3)}-**";
        }
    }
}
