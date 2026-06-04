# ADR-0005 — IR no Kafka publicado após o commit

**Status:** Aceito

## Contexto
Publicar o evento de IR antes de persistir (dual-write) corre o risco de o tópico conter
eventos que não foram salvos, caso o `SaveChanges` falhe.

## Decisão
O motor coleta os `EventoIR`, persiste tudo numa transação e **só então** publica no Kafka
(`corretora-ir-events`). Cada mensagem carrega o CPF (RN-056).

## Consequências
- (+) O tópico reflete apenas eventos efetivamente persistidos.
- (+) Consumidores recebem dados consistentes com o banco.
- (−) Entrega *at-least-once* na falha pós-commit → consumidores devem ser idempotentes.
  Outbox transacional fica como evolução, fora do escopo atual (simplicidade).
