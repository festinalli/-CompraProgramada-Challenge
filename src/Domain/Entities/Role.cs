using System.Collections.Generic;

namespace Domain.Entities
{
    /// <summary>Papel (perfil) que agrupa permissões. Relacionamento N:N com Usuario e Permission.</summary>
    public class Role
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;

        public ICollection<Permission> Permissions { get; set; } = new List<Permission>();
        public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    }

    /// <summary>Permissão fina (recurso:ação), ex.: "motor:executar".</summary>
    public class Permission
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;

        public ICollection<Role> Roles { get; set; } = new List<Role>();
    }
}
