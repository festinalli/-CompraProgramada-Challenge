# Arquitetura — C4

Modelo C4 (Context → Container → Component) do Sistema de Compra Programada.
Diagramas em Mermaid (renderizam no GitHub). Decisões em [`docs/adr`](../adr).

## Nível 1 — Contexto

```mermaid
C4Context
title Compra Programada de Ações — Contexto
Person(cliente, "Investidor", "Adere ao produto, acompanha carteira e rentabilidade")
Person(staff, "Backoffice", "Administrador / Operador: cesta Top Five e motor de compra")
System(sistema, "Compra Programada", "Automatiza aportes mensais na cesta recomendada, com IR e rebalanceamento")
System_Ext(b3, "COTAHIST (B3)", "Arquivo com cotações de fechamento")
System_Ext(downstream, "Consumidores de IR", "Sistemas fiscais a jusante (via Kafka)")
Rel(cliente, sistema, "Usa", "HTTPS")
Rel(staff, sistema, "Opera", "HTTPS")
Rel(sistema, b3, "Lê cotação de fechamento")
Rel(sistema, downstream, "Publica eventos de IR", "Kafka")
```

## Nível 2 — Containers

```mermaid
C4Container
title Compra Programada — Containers
Person(cliente, "Investidor")
Person(staff, "Backoffice")
Container_Boundary(b, "Compra Programada") {
  Container(spa, "Frontend", "Angular 17 + Nginx", "Telas de cliente e admin; JWT no header")
  Container(api, "API REST", ".NET 8", "CQRS, JWT + RBAC, motor e rebalanceamento")
  ContainerDb(db, "MySQL 8", "EF Core Migrations", "Clientes, custódias, cestas, RBAC, eventos de IR")
  ContainerQueue(kafka, "Kafka", "Confluent", "Tópico corretora-ir-events")
}
System_Ext(cota, "COTAHIST", "Arquivo de cotações da B3")
Rel(cliente, spa, "HTTPS")
Rel(staff, spa, "HTTPS")
Rel(spa, api, "JSON", "/api/* via proxy Nginx (mesma origem)")
Rel(api, db, "EF Core", "TCP/3306")
Rel(api, kafka, "Publica IR (pós-commit)", "TCP/9092")
Rel(api, cota, "Lê cotação de fechamento")
```

## Nível 3 — Componentes (dentro da API)

```mermaid
C4Component
title API .NET — Componentes
Container_Boundary(api, "API .NET 8") {
  Component(ctrl, "Controllers", "ASP.NET Core", "Auth, Clientes, Admin, Motor")
  Component(authz, "Autorização", "PermissionPolicyProvider + Handler", "Permissões finas (claim) no JWT; defesa IDOR")
  Component(cqrs, "Handlers CQRS", "MediatR", "Adesão, Carteira/Rentabilidade, Cesta, Histórico, Custódia")
  Component(motor, "MotorCompraService", "Application", "Consolida, lote padrão/fracionário, distribui, resíduo")
  Component(rebal, "RebalanceamentoService", "Application", "Troca de cesta / desvio de proporção + IR mensal")
  Component(dom, "Domínio", "CalculadoraIR, CalendarioPregao", "Regras puras e testáveis")
  Component(abs, "Abstrações", "IAppDbContext, ICotahistParser, IKafkaProducer, ICotacaoProvider", "Contratos da Application")
  Component(infra, "Infraestrutura", "EF Core, KafkaProducer, CotahistParser", "Implementa as abstrações")
}
ContainerDb(db, "MySQL")
ContainerQueue(kafka, "Kafka")
Rel(ctrl, authz, "exige permissão")
Rel(ctrl, cqrs, "envia comando/query")
Rel(cqrs, motor, "usa")
Rel(cqrs, rebal, "usa")
Rel(motor, dom, "usa")
Rel(rebal, dom, "usa")
Rel(motor, abs, "depende de")
Rel(infra, abs, "implementa")
Rel(infra, db, "EF Core")
Rel(infra, kafka, "Confluent.Kafka")
```

## Dependências entre camadas (inversão)

```
API → Application → Domain
Infrastructure → Application → Domain
```

`Application` **não** referencia `Infrastructure`: depende de abstrações que ela mesma define
(`src/Application/Abstractions`), implementadas pela Infraestrutura. Dependências apontam para
dentro (Clean Architecture). Detalhe em [ADR-0003](../adr/0003-inversao-de-dependencia.md).
