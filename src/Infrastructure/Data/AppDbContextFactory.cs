using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Data
{
    /// <summary>
    /// Factory de design-time para o EF Core CLI (dotnet ef migrations).
    /// Usa uma versão fixa do MySQL para não depender de um banco em execução
    /// ao gerar/scriptar migrations (evita o connect do ServerVersion.AutoDetect).
    /// </summary>
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var connectionString =
                Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                ?? "Server=localhost;Port=3306;Database=corretora;Uid=root;Pwd=root;";

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36)))
                .Options;

            return new AppDbContext(options);
        }
    }
}
