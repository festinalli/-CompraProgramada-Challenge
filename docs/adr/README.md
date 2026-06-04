# Architecture Decision Records (ADR)

Registro das decisões arquiteturais relevantes. Formato curto: Contexto · Decisão · Consequências.

| ADR | Título |
|-----|--------|
| [0001](0001-clean-architecture-cqrs.md) | Clean Architecture + CQRS (MediatR) |
| [0002](0002-ef-migrations-fonte-unica.md) | EF Core Migrations como fonte única do schema |
| [0003](0003-inversao-de-dependencia.md) | Inversão de dependência (Application ⟂ Infrastructure) |
| [0004](0004-regras-financeiras-no-dominio.md) | Regras financeiras no domínio (puras e testáveis) |
| [0005](0005-kafka-publicacao-pos-commit.md) | IR no Kafka publicado após o commit |
| [0006](0006-authn-authz-jwt-rbac.md) | AuthN/AuthZ: JWT + RBAC com permissões finas |
| [0007](0007-hardening-sem-credenciais-default.md) | Hardening: sem credenciais default, PBKDF2, defesa IDOR |
