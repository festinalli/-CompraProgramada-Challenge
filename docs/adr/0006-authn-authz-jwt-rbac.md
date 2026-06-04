# ADR-0006 — AuthN/AuthZ: JWT + RBAC com permissões finas

**Status:** Aceito

## Contexto
Um sistema de corretora precisa separar investidores de backoffice e autorizar por capacidade,
não só por papel. O alvo será submetido a pentest.

## Decisão
- **Identidades distintas:** `Cliente` (investidor, login por CPF) e `Usuario` (backoffice).
- **RBAC:** `Usuario *—* Role *—* Permission` (N:N). O JWT carrega papéis (claim role) e
  **permissões finas** (claim `permission`, ex.: `motor:executar`, `cesta:escrever`).
- **Autorização por política dinâmica:** `[HasPermission("...")]` + `PermissionPolicyProvider`
  resolve cada permissão como policy, sem registrar uma a uma.
- Cliente recebe papel `Cliente` + `carteira:ler` (restrito ao próprio id — ver ADR-0007).

## Consequências
- (+) Autorização declarativa e granular; fácil adicionar permissões/papéis (ex.: Operador).
- (+) Auditável (permissões explícitas no token e nos endpoints).
- (−) Dois fluxos de login (cliente/backoffice) a manter.
