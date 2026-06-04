# ADR-0004 — Regras financeiras no domínio (puras e testáveis)

**Status:** Aceito

## Contexto
Regras como IR (dedo-duro 0,005%, isenção mensal de R$ 20.000, 20% sobre lucro) e o calendário
de pregão (dias 5/15/25 → próximo dia útil) estavam embutidas em serviços, difíceis de testar.

## Decisão
Extrair as regras para funções puras no `Domain`: `CalculadoraIR` e `CalendarioPregao`. Os
serviços de aplicação as consomem; o preço médio fica encapsulado em `CustodiaFilhote`.

## Consequências
- (+) Cada borda testada por unidade (isenção, prejuízo→R$0, dia útil, etc.) sem I/O.
- (+) Regra com um único dono, sem duplicação.
- (−) Mais um ponto de indireção entre serviço e cálculo (aceitável).
