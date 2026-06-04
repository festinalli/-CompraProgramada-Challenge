using Application.Services.Security;
using Domain.Entities;
using Domain.Security;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Seed
{
    /// <summary>
    /// Seed idempotente executado após Migrate(). Semeia o vocabulário de RBAC
    /// (permissões + papéis) e a cesta de referência. O usuário admin é criado
    /// SOMENTE a partir de configuração (Admin:Username/Password), com política de
    /// senha — não há credencial default (requisito de hardening/pentest).
    /// </summary>
    public static class DbSeeder
    {
        private const int SenhaAdminMinima = 12;

        public static async Task SeedAsync(
            AppDbContext context,
            IPasswordHasher passwordHasher,
            IConfiguration configuration,
            ILogger logger)
        {
            await SeedPermissoesEPapeis(context, logger);
            await SeedAdmin(context, passwordHasher, configuration, logger);
            await SeedCestaReferencia(context, logger);
            await context.SaveChangesAsync();
        }

        private static async Task SeedPermissoesEPapeis(AppDbContext context, ILogger logger)
        {
            foreach (var nome in Permissions.Todas)
                if (!await context.Permissions.AnyAsync(p => p.Nome == nome))
                    context.Permissions.Add(new Permission { Nome = nome });
            await context.SaveChangesAsync();

            foreach (var (roleNome, perms) in RolesPadrao.Permissoes)
            {
                var role = await context.Roles.Include(r => r.Permissions)
                    .FirstOrDefaultAsync(r => r.Nome == roleNome);
                if (role == null)
                {
                    role = new Role { Nome = roleNome };
                    context.Roles.Add(role);
                }

                var jaTem = role.Permissions.Select(p => p.Nome).ToHashSet();
                foreach (var pn in perms.Where(pn => !jaTem.Contains(pn)))
                    role.Permissions.Add(await context.Permissions.FirstAsync(p => p.Nome == pn));
            }
            await context.SaveChangesAsync();
            logger.LogInformation("🌱 RBAC semeado: {Perms} permissões, {Roles} papéis",
                Permissions.Todas.Length, RolesPadrao.Permissoes.Count);
        }

        private static async Task SeedAdmin(
            AppDbContext context, IPasswordHasher hasher, IConfiguration config, ILogger logger)
        {
            var username = config["Admin:Username"];
            var password = config["Admin:Password"];

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                logger.LogWarning("⚠️ Admin:Username/Password não configurados — nenhum admin criado. " +
                                  "Defina as variáveis de ambiente para provisionar o administrador.");
                return;
            }
            if (password.Length < SenhaAdminMinima)
            {
                logger.LogWarning("⚠️ Admin:Password abaixo da política ({Min}+ caracteres). Admin NÃO criado.",
                    SenhaAdminMinima);
                return;
            }
            if (await context.Usuarios.AnyAsync(u => u.Username == username))
                return;

            var admin = new Usuario { Username = username, Nome = "Administrador", SenhaHash = hasher.Hash(password) };
            admin.Roles.Add(await context.Roles.FirstAsync(r => r.Nome == RolesPadrao.Administrador));
            context.Usuarios.Add(admin);
            logger.LogInformation("🌱 Usuário admin '{Username}' provisionado", username);
        }

        private static async Task SeedCestaReferencia(AppDbContext context, ILogger logger)
        {
            if (await context.CestasRecomendacao.AnyAsync(c => c.Ativa))
                return;

            context.CestasRecomendacao.Add(new CestaRecomendacao
            {
                Nome = "TOP FIVE",
                Ativa = true,
                DataCriacao = DateTime.UtcNow,
                Itens = new List<ItemCesta>
                {
                    new() { Ticker = "PETR4", Percentual = 20 },
                    new() { Ticker = "VALE3", Percentual = 20 },
                    new() { Ticker = "ITUB4", Percentual = 20 },
                    new() { Ticker = "BBDC4", Percentual = 20 },
                    new() { Ticker = "WEGE3", Percentual = 20 },
                }
            });
            logger.LogInformation("🌱 Cesta Top Five de referência criada");
        }
    }
}
