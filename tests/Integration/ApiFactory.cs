using Application.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Messaging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Integration
{
    /// <summary>
    /// Sobe a API real (todo o pipeline) trocando MySQL por InMemory e o Kafka
    /// por um fake (sem broker). Usa o fixture COTAHIST copiado para o output.
    ///
    /// Config via variáveis de ambiente: o Program lê JWT/ConnectionString em
    /// tempo de build (antes de ConfigureAppConfiguration aplicar), então as
    /// variáveis de ambiente garantem a MESMA chave em assinatura e validação.
    /// </summary>
    public class ApiFactory : WebApplicationFactory<Program>
    {
        // Nome estável por fábrica: todos os escopos compartilham o mesmo banco InMemory.
        private readonly string _dbName = "itests-" + Guid.NewGuid();

        public ApiFactory()
        {
            Environment.SetEnvironmentVariable("JWT__SecretKey", "integration-tests-secret-key-min-32-characters-0001");
            Environment.SetEnvironmentVariable("JWT__Issuer", "corretora-test");
            Environment.SetEnvironmentVariable("JWT__Audience", "corretora-test");
            Environment.SetEnvironmentVariable("JWT__ExpirationMinutes", "60");
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Server=localhost;Database=t;Uid=t;Pwd=t;");
            Environment.SetEnvironmentVariable("Kafka__BootstrapServers", "localhost:9092");
            Environment.SetEnvironmentVariable("Cotacoes__PastaLocal", Path.Combine(AppContext.BaseDirectory, "cotacoes"));
            Environment.SetEnvironmentVariable("Admin__Username", "admin");
            Environment.SetEnvironmentVariable("Admin__Password", "Test#Admin#2026"); // ≥12, política de senha
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                // Remove o DbContext (MySQL) e injeta InMemory isolado por fábrica.
                foreach (var d in services.Where(d =>
                             d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                             d.ServiceType == typeof(DbContextOptions) ||
                             d.ServiceType == typeof(AppDbContext)).ToList())
                    services.Remove(d);

                services.AddDbContext<AppDbContext>(o =>
                    o.UseInMemoryDatabase(_dbName)
                     .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

                // Kafka fake (captura eventos publicados, sem broker).
                foreach (var d in services.Where(d => d.ServiceType == typeof(IKafkaProducer)).ToList())
                    services.Remove(d);
                services.AddSingleton<IKafkaProducer, FakeKafkaProducer>();
            });
        }
    }

    public class FakeKafkaProducer : IKafkaProducer
    {
        public List<EventoIR> Publicados { get; } = new();

        public Task PublicarEventoIR(EventoIR evento)
        {
            Publicados.Add(evento);
            return Task.CompletedTask;
        }
    }
}
