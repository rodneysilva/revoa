# ADR-0008: OOUX objects-first (ORCA) como metodologia de design

## Status
Accepted — baseline travado no Plano (§1, decisão #22; seção 10).

## Contexto
Produtos falham quando o design começa por **telas/fluxos** (page-first, actions-first) antes de
modelar os **objetos** que o usuário tem na cabeça. O revoa tem complexidade de modelo (Anúncio com
kind, Troca/escrow, Comunidade/posts recursivos, Carteira/RVM) — começar pela UI gera retrabalho e
inconsistência entre UX e domínio.

## Decisão
Adotar **OOUX (Object-Oriented UX)** com o processo **ORCA** (Objects → Relationships → CTAs →
Attributes/Views/States) como metodologia de design:
- **Toda feature nova** começa pelo mapa de objetos (`docs/OOUX.md`).
- Skill dedicada: `.kilo/skills/ooux/SKILL.md`.
- **Cada objeto OOUX → um aggregate DDD** (alinhamento UX↔domínio).
- YAGNI aplicado: objeto sem CTA/estado = atributo ou excluído.

## Alternativas consideradas
- **Design page-first / actions-first:** anti-padrão documentado (retrabalho, UX≠domínio).
- **DDD puro sem OOUX:** aggregate pode divergir do modelo mental do usuário (UI desconexa).
- **Sem metodologia formal:** inconsistência entre features/contribuidores.

## Consequências
- **+:** UX e domínio alinhados (objeto OOUX = aggregate); menos retrabalho; onboarding de contribuidores claro.
- **+:** YAGNI enforced (só objetos com CTA real viram aggregate).
- **−:** disciplina para SEMPRE atualizar `docs/OOUX.md` antes de codar.
- **−:** overhead de processo em features triviais (mitigado: ajustes cosméticos dispensam OOUX).
