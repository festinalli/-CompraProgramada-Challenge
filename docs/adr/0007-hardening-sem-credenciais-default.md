# ADR-0007 — Hardening: sem credenciais default, PBKDF2, defesa IDOR

**Status:** Aceito

## Contexto
Preparação para pentest. Credenciais default, hash fraco e acesso a recursos de terceiros
(IDOR) são achados clássicos.

## Decisão
- **Sem credencial default:** o admin é provisionado **apenas** via `Admin:Username/Password`
  (env), com política de senha (≥ 12). Sem env → nenhum admin é criado. Nada de demo seedado.
- **Senhas:** PBKDF2 (HMAC-SHA256, salt por usuário, 100k iterações), verificação em tempo
  constante. Login não revela se o identificador existe.
- **Defesa IDOR:** endpoints `/clientes/{id}/*` checam posse — o cliente só acessa o próprio id;
  backoffice (Administrador/Operador) acessa qualquer um. Caso contrário, **403**.
- **Transporte/segredos:** JWT assinado (HS256, chave ≥ 32, UTF-8); `RequireHttpsMetadata` fora
  de Dev; CORS por origem; segredos em `.env` gitignored.

## Consequências
- (+) Reduz a superfície de achados típicos de pentest.
- (+) Provisionamento explícito e auditável.
- (−) O ambiente precisa definir o admin antes do 1º uso (documentado no README/.env.example).
