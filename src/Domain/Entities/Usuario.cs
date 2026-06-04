using System.Collections.Generic;

namespace Domain.Entities
{
    /// <summary>
    /// Usuário operacional do sistema (backoffice). Separado de Cliente: Cliente é quem
    /// investe (tem CPF/custódia); Usuario é quem opera o backoffice, com papéis e permissões.
    /// </summary>
    public class Usuario
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string SenhaHash { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;

        public ICollection<Role> Roles { get; set; } = new List<Role>();
    }
}
