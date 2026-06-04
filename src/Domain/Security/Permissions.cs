namespace Domain.Security
{
    /// <summary>
    /// Vocabulário de permissões finas do sistema (recurso:ação). As permissões são
    /// transportadas no JWT como claims e verificadas por política de autorização.
    /// </summary>
    public static class Permissions
    {
        public const string CestaLer = "cesta:ler";
        public const string CestaEscrever = "cesta:escrever";
        public const string MotorExecutar = "motor:executar";
        public const string CustodiaLer = "custodia:ler";
        public const string CarteiraLer = "carteira:ler";

        public static readonly string[] Todas =
        {
            CestaLer, CestaEscrever, MotorExecutar, CustodiaLer, CarteiraLer
        };
    }

    /// <summary>Papéis padrão. Staff são usuários do backoffice; Cliente é o investidor.</summary>
    public static class RolesPadrao
    {
        public const string Administrador = "Administrador";
        public const string Operador = "Operador";
        public const string Cliente = "Cliente";

        /// <summary>Permissões concedidas a cada papel de backoffice (semeadas no startup).</summary>
        public static readonly IReadOnlyDictionary<string, string[]> Permissoes =
            new Dictionary<string, string[]>
            {
                [Administrador] = new[]
                {
                    Permissions.CestaLer, Permissions.CestaEscrever,
                    Permissions.MotorExecutar, Permissions.CustodiaLer, Permissions.CarteiraLer
                },
                // Operador opera o dia a dia mas NÃO altera a cesta recomendada.
                [Operador] = new[]
                {
                    Permissions.CestaLer, Permissions.MotorExecutar, Permissions.CustodiaLer
                }
            };
    }
}
