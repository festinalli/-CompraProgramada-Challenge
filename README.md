# Compra Programada de Ações

> **Aviso:** projeto de estudo desenvolvido a partir de um desafio técnico público de engenharia
> de software. Sem qualquer afiliação, patrocínio ou endosso de instituições financeiras. Marcas,
> tickers e dados de mercado citados pertencem aos respectivos titulares.

Sistema que automatiza o investimento recorrente numa carteira recomendada ("Top Five",
5 ações = 100%). Clientes aportam mensalmente; o valor é dividido em 3 ciclos (dias 5/15/25,
ou o próximo dia útil), consolidado na conta master e distribuído proporcionalmente às contas
filhote, com cálculo de IR (dedo-duro e sobre lucro) publicado em Kafka.

## Stack

- **Backend:** .NET 8, Clean Architecture + CQRS (MediatR), FluentValidation
- **Persistência:** MySQL 8 via EF Core (**Migrations** como fonte única do schema)
- **Mensageria:** Apache Kafka (Confluent)
- **Cotações:** parser do arquivo COTAHIST da B3
- **API:** REST + Swagger/OpenAPI, autenticação JWT
- **Frontend:** Angular 17 (standalone components) + Tailwind
- **Infra:** Docker Compose + Nginx

## Arquitetura (camadas)

```
Domain          entidades + regras puras (CalculadoraIR, CalendarioPregao)
Application     casos de uso CQRS (handlers), serviços (motor, rebalanceamento)
Infrastructure  EF Core (AppDbContext + Migrations), Kafka, parser COTAHIST
API             controllers REST, JWT, seed, Swagger
```

📐 **Diagramas C4** (Context/Container/Component) em [`docs/architecture`](docs/architecture/README.md)
· **ADRs** (decisões) em [`docs/adr`](docs/adr/README.md).

## Como executar (1 comando)

Pré-requisito: Docker.

```bash
cd docker
cp .env.example .env            # ajuste os segredos se quiser
docker compose up -d --build
```

Sobe MySQL, Kafka/Zookeeper, a API (que **migra e seeda** o banco no startup) e o frontend.

- **Frontend:** http://localhost
- **Swagger:** http://localhost:5000/swagger
- **API:** http://localhost:5000

> **Portas ocupadas?** Se 5000/3306/9092/80 já estiverem em uso na sua máquina, suba com o
> override de portas alternativas (frontend 8090, API 5080, MySQL 3307, Kafka 9094):
> ```bash
> docker compose -f docker-compose.yml -f compose.local.yml up -d --build
> ```
> A comunicação entre containers é interna e não muda; só as portas publicadas no host.

### Acesso (sem credenciais default — hardening/pentest)

- **Administrador:** provisionado **apenas** a partir de `Admin:Username` / `Admin:Password`
  no `docker/.env` (senha ≥ 12 caracteres). Se não definir, **nenhum** admin é criado.
  Login no front em "Administrador".
- **Cliente (investidor):** cria a própria conta via **/adesao** (ou `POST /api/clientes/adesao`)
  e faz login com CPF + senha.
- No 1º boot são semeados o vocabulário de **RBAC** (papéis Administrador/Operador + permissões
  finas) e a cesta "Top Five" de referência — nenhuma credencial é semeada.

> Segredos (JWT, senhas, conexão) ficam em `docker/.env` (gitignored). Veja `docker/.env.example`.

## Endpoints

| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/api/auth/login` · `/api/auth/login-admin` | Autenticação (JWT) |
| POST | `/api/clientes/adesao` | Adesão ao produto |
| POST | `/api/clientes/{id}/saida` | Saída (mantém posição) |
| PUT  | `/api/clientes/{id}/valor-mensal` | Alterar aporte |
| GET  | `/api/clientes/{id}/carteira` | Carteira (posições, P/L) |
| GET  | `/api/clientes/{id}/rentabilidade` | Rentabilidade detalhada |
| POST | `/api/admin/cesta` | Cadastrar/alterar cesta (dispara rebalanceamento) |
| GET  | `/api/admin/cesta/atual` · `/historico` | Cesta atual / histórico |
| GET  | `/api/admin/conta-master/custodia` | Resíduos da custódia master |
| POST | `/api/motor/executar-compra` | Executa o motor (`{ dataReferencia }`) |

## Regras de negócio (resumo)

- Compra nos dias 5/15/25 (→ próximo dia útil); 1/3 do aporte por ciclo; só clientes ativos.
- Quantidade = `floor(valor / preçoFechamento)`; abate o saldo da custódia master.
- Lote padrão (múltiplos de 100) + fracionário (ticker + sufixo `F`).
- Distribuição proporcional ao aporte; resíduo permanece na master.
- Preço médio ponderado (atualizado só em compra; venda não altera).
- IR dedo-duro 0,005% por operação → Kafka (com CPF).
- IR sobre lucro: vendas do mês ≤ R$ 20.000 isentas; acima, 20% do lucro (prejuízo → R$ 0).
- Trocar a cesta dispara rebalanceamento dos clientes ativos.

## Testes e cobertura

```bash
dotnet test tests/Tests.csproj --settings tests/coverage.runsettings --collect:"XPlat Code Coverage"
```

Inclui testes de domínio (IR, dia útil, preço médio), de serviços (motor, rebalanceamento),
do parser COTAHIST (fixture B3 real) e de **integração** (sobe a API com `WebApplicationFactory`,
exercitando os endpoints ponta a ponta). Cobertura de linhas **~80%** (migrations excluídas).

## Desenvolvimento local (sem Docker)

```bash
# Backend (requer MySQL e Kafka acessíveis; veja appsettings)
dotnet run --project src/API/API.csproj
# Frontend
cd frontend && npm install && npm start
```
