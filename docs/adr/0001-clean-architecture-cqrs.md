# ADR-0001 — Clean Architecture + CQRS (MediatR)

**Status:** Aceito

## Contexto
Sistema com regras de negócio sensíveis (motor de compra, IR, rebalanceamento) que precisam
ser testáveis isoladamente e evoluir sem acoplamento à infraestrutura.

## Decisão
Quatro camadas — `Domain`, `Application`, `Infrastructure`, `API` — com CQRS na Application via
**MediatR** (comandos/queries com handlers dedicados). Controllers apenas orquestram.

## Consequências
- (+) Fronteiras claras; handlers pequenos e testáveis; intenção explícita (comando vs query).
- (+) Domínio sem dependência de framework.
- (−) Mais arquivos/cerimônia que um CRUD direto — justificado pela complexidade do negócio.
