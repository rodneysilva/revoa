# ADR-0012: Backend único .NET; frontend único React/TypeScript

## Status
Accepted — regra de stack do dono (explicitada na rodada v3.0).

## Contexto
O workspace tem múltiplos projetos em stacks diversas (Python no trocadeira, .NET no equivale, etc.).
Para reduzir complexidade, padronizar manutenção e reuso de conhecimento, o dono definiu que o revoa
usa **uma única linguagem de backend (.NET)** e **um único framework de frontend (React com TypeScript)**.
Outras linguagens (Python, etc.) não devem ser introduzidas no revoa (exceto scripts auxiliares isolados).

## Decisão
- **Backend:** **.NET 10** único (host ASP.NET + módulos + Indexer). Sem Python/Node/Go no backend.
- **Frontend:** **React + TypeScript** (Vite SPA + PWA). Sem SolidJS/Vue/Angular.
- **Exceções permitidas:** Solidity (contratos), SQL/Mongo queries, scripts de build/dev (ex.: o
  `render_explorations.py` de geração de assets é ferramenta isolada, não parte do produto).
- **Motivação:** conhecimento concentrado, contratação/colaboração mais fácil, menos context-switch.

## Alternativas consideradas
- **Microsserviços poliglotas:** rejeitado (complexidade, contradiz ADR-0003 monólito modular).
- **SolidJS (reusar equivale):** código frontend não reusável; decisão do dono = React (ADR-0006).

## Consequências
- **+:** stack homogênea; manutenção/CI simpler; reuso de padrões.
- **−:** não aproveita código Python do trocadeira (só documentação/regras de negócio) — aceito (sunset).
- **−:** bibliotecas específicas de outra linguagem exigem equivalente em .NET/TS (verificar antes de adotar).
