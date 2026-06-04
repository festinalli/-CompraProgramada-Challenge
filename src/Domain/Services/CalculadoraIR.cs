namespace Domain.Services
{
    /// <summary>
    /// Regras de IR centralizadas (pura, testável):
    /// - Dedo-duro: 0,005% por transação.
    /// - Lucro: vendas do mês ≤ R$ 20.000 isentas; acima, 20% sobre o lucro (prejuízo → R$ 0).
    /// </summary>
    public static class CalculadoraIR
    {
        public const decimal AliquotaDedoDuro = 0.00005m;   // 0,005%
        public const decimal AliquotaLucro = 0.20m;         // 20%
        public const decimal LimiteIsencaoMensal = 20_000m;

        public static decimal DedoDuro(decimal valorOperacao)
            => valorOperacao <= 0 ? 0m : valorOperacao * AliquotaDedoDuro;

        /// <summary>
        /// IR sobre lucro mensal. Isento se o total de vendas do mês ≤ R$ 20.000
        /// ou se houver prejuízo; caso contrário, 20% sobre o lucro.
        /// </summary>
        public static decimal IrSobreLucroMensal(decimal totalVendasMes, decimal lucro)
            => totalVendasMes > LimiteIsencaoMensal && lucro > 0 ? lucro * AliquotaLucro : 0m;
    }
}
