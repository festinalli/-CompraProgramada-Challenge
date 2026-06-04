# ADR-0003 — Inversão de dependência (Application ⟂ Infrastructure)

**Status:** Aceito

## Contexto
A `Application` referenciava a `Infrastructure` e os serviços dependiam do `AppDbContext`
concreto — dependência apontando para fora, contra a Clean Architecture.

## Decisão
A `Application` define as abstrações em `src/Application/Abstractions` (`IAppDbContext`,
`ICotahistParser`, `ICotacaoProvider`, `IKafkaProducer`). A `Infrastructure` referencia a
`Application` e **implementa** essas abstrações. `Application` depende só de `Domain`.

## Consequências
- (+) Dependências apontam para dentro; Application testável sem EF/Kafka reais.
- (+) Trocar a implementação (ex.: provider de cotação) não toca a Application.
- (−) `IAppDbContext` expõe `DbSet<>`/`DatabaseFacade` (abstração com sabor EF) — trade-off
  pragmático ante criar repositórios para cada agregado.
