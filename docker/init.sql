-- =====================================================================
-- init.sql — Bootstrap mínimo do MySQL para a Compra Programada de Ações.
--
-- IMPORTANTE: o SCHEMA (tabelas) é gerenciado EXCLUSIVAMENTE por EF Core
-- Migrations (src/Infrastructure/Data/Migrations), aplicadas no startup da
-- API via db.Database.Migrate(). Este arquivo só garante o database, o
-- usuário da aplicação e os privilégios — NÃO cria tabelas nem faz seed.
-- =====================================================================

CREATE DATABASE IF NOT EXISTS corretora
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

CREATE USER IF NOT EXISTS 'corretora_user'@'%' IDENTIFIED BY 'corretora_password';
GRANT ALL PRIVILEGES ON corretora.* TO 'corretora_user'@'%';
FLUSH PRIVILEGES;
