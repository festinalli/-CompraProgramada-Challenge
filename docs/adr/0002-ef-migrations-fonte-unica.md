# ADR-0002 — EF Core Migrations como fonte única do schema

**Status:** Aceito

## Contexto
Havia duas fontes de schema divergentes: um `init.sql` com tabelas e o modelo EF. Isso causa
drift e bugs ("tabela já existe", colunas ausentes).

## Decisão
Migrations do EF Core são a **única** fonte do schema, aplicadas no startup com
`db.Database.Migrate()`. O `init.sql` foi reduzido a criar database/usuário. Um
`IDesignTimeDbContextFactory` com versão fixa do MySQL permite gerar migrations sem banco no ar.

## Consequências
- (+) Schema versionado, reprodutível, sem divergência.
- (+) `docker compose up` migra e seeda sozinho.
- (−) Trocar `EnsureCreated()`→`Migrate()` exige recriar volumes antigos (`down -v`).
