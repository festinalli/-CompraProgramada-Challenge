using Application.Abstractions;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests.Integration
{
    public class EndpointsIntegrationTests
    {
        private const string AdminUser = "admin";
        private const string AdminPass = "Test#Admin#2026";

        private record LoginResp(string token, int clienteId, string nome);

        private static void Auth(HttpClient c, string token) =>
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        private static async Task<string> LoginAdmin(HttpClient c)
        {
            var resp = await c.PostAsJsonAsync("/api/auth/login-admin", new { usuario = AdminUser, senha = AdminPass });
            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            return (await resp.Content.ReadFromJsonAsync<LoginResp>())!.token;
        }

        private static async Task<int> RegistrarCliente(HttpClient c, string cpf, string senha = "SenhaForte1")
        {
            var resp = await c.PostAsJsonAsync("/api/clientes/adesao", new
            {
                cpf, nome = "Cliente Teste", email = $"{cpf}@test.com", valorMensal = 3000m, senha
            });
            resp.StatusCode.Should().Be(HttpStatusCode.Created);
            return (await resp.Content.ReadFromJsonAsync<LoginResp>())!.clienteId;
        }

        private static async Task<LoginResp> LoginCliente(HttpClient c, string cpf, string senha = "SenhaForte1")
        {
            var resp = await c.PostAsJsonAsync("/api/auth/login", new { CPF = cpf, Senha = senha });
            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            return (await resp.Content.ReadFromJsonAsync<LoginResp>())!;
        }

        [Fact]
        public async Task FluxoCompleto_Adesao_Motor_Carteira_Rentabilidade()
        {
            using var factory = new ApiFactory();
            var client = factory.CreateClient();

            // 1. Cliente se registra (sem credencial seedada)
            await RegistrarCliente(client, "52998224725");

            // 2. Admin (provisionado por env) opera o backoffice
            Auth(client, await LoginAdmin(client));
            (await client.GetAsync("/api/admin/cesta/atual")).StatusCode.Should().Be(HttpStatusCode.OK);

            // 3. Executa o motor (cliente ativo recebe posições)
            (await client.PostAsJsonAsync("/api/motor/executar-compra", new { dataReferencia = "2026-03-05" }))
                .StatusCode.Should().Be(HttpStatusCode.OK);

            // 3a. Eventos de IR dedo-duro publicados no Kafka, com CPF (RN-056)
            var kafka = (FakeKafkaProducer)factory.Services.GetRequiredService<IKafkaProducer>();
            kafka.Publicados.Should().Contain(e => e.Tipo == global::Domain.Entities.TipoIR.DEDO_DURO);
            kafka.Publicados.Should().OnlyContain(e => !string.IsNullOrEmpty(e.CPF));

            // 4. Trocar a cesta → rebalanceamento dos ativos
            (await client.PostAsJsonAsync("/api/admin/cesta", new
            {
                nome = "Top Five v2",
                ativos = new[]
                {
                    new { ticker = "PETR4", percentual = 20m }, new { ticker = "VALE3", percentual = 20m },
                    new { ticker = "ITUB4", percentual = 20m }, new { ticker = "BBDC4", percentual = 20m },
                    new { ticker = "PETR3", percentual = 20m },
                }
            })).StatusCode.Should().Be(HttpStatusCode.Created);

            (await client.GetAsync("/api/admin/conta-master/custodia")).StatusCode.Should().Be(HttpStatusCode.OK);
            (await client.GetAsync("/api/admin/cesta/historico")).StatusCode.Should().Be(HttpStatusCode.OK);

            // 5. Cliente loga e vê a própria carteira/rentabilidade
            var cli = await LoginCliente(client, "52998224725");
            Auth(client, cli.token);
            (await client.GetAsync($"/api/clientes/{cli.clienteId}/carteira")).StatusCode.Should().Be(HttpStatusCode.OK);
            (await client.GetAsync($"/api/clientes/{cli.clienteId}/rentabilidade")).StatusCode.Should().Be(HttpStatusCode.OK);

            // 6. Self-service do cliente
            (await client.PutAsJsonAsync($"/api/clientes/{cli.clienteId}/valor-mensal", new { novoValorMensalAporte = 1500m }))
                .StatusCode.Should().Be(HttpStatusCode.OK);
            (await client.PostAsJsonAsync($"/api/clientes/{cli.clienteId}/saida", new { motivo = "teste" }))
                .StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Cliente_NaoAcessaCarteiraDeOutro_Retorna403_IDOR()
        {
            using var factory = new ApiFactory();
            var client = factory.CreateClient();
            await RegistrarCliente(client, "52998224725");
            var cli = await LoginCliente(client, "52998224725");
            Auth(client, cli.token);

            // Tenta ler a carteira de OUTRO cliente → proibido (defesa contra IDOR)
            var outroId = cli.clienteId + 999;
            (await client.GetAsync($"/api/clientes/{outroId}/carteira")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Cliente_NaoExecutaMotor_Retorna403_RBAC()
        {
            using var factory = new ApiFactory();
            var client = factory.CreateClient();
            await RegistrarCliente(client, "52998224725");
            Auth(client, (await LoginCliente(client, "52998224725")).token);

            // Cliente não tem a permissão motor:executar
            (await client.PostAsJsonAsync("/api/motor/executar-compra", new { dataReferencia = "2026-03-05" }))
                .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Login_SenhaErrada_Retorna401()
        {
            using var factory = new ApiFactory();
            var client = factory.CreateClient();
            await RegistrarCliente(client, "52998224725");
            var resp = await client.PostAsJsonAsync("/api/auth/login", new { CPF = "52998224725", Senha = "errada" });
            resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task EndpointAdminProtegido_SemToken_Retorna401()
        {
            using var factory = new ApiFactory();
            var client = factory.CreateClient();
            (await client.GetAsync("/api/admin/cesta/atual")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
