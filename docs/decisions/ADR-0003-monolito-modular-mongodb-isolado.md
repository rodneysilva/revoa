# ADR-0003: Monólito modular com MongoDB isolado por coleção

## Status
Accepted — baseline travado no Plano (§1, decisão #13–14).

## Contexto
O revoa tem 12 bounded contexts (Identity, Account, Catalog, Exchange, Token, Community, ...).
Decisão inicial: microsserviços vs monólito. O sistema é novo (greenfield), sem tráfego conhecido,
equipe pequena. Microsserviços prematuros = complexidade operacional (deploy, observabilidade,
consistência distribuída) sem benefício. Mas queremos **preservar a extração futura**.

## Decisão
**Monólito modular** (.NET único host):
- Módulos isolados, cada um com **coleções MongoDB próprias**.
- **PROIBIDO query cross-coleção** entre módulos.
- Comunicação só via **`IIntegrationEventBus`** (in-process, MediatR agora → MassTransit ao extrair).
- **CQRS + MediatR**; domínio puro.
- MongoDB global (replica set `rs0`), **optimistic locking (`Version`)** + **ACID seletiva** só na
  finalização financeira (blueprint `equivale`).

## Alternativas consideradas
- **Microsserviços desde o início:** over-engineering (viola YAGNI); complexidade operacional prematura.
- **Monólito "big ball of mud" (sem isolamento):** acoplamento impede extração futura; difícil testar.
- **PostgreSQL/relacional:** o blueprint (equivale) provou MongoDB; schemas flexíveis (VOs por kind,
  posts materialized path) se beneficiam de documento.

## Consequências
- **+:** deploy/observabilidade simples agora; isolamento preserva extração a microsserviço (bus → MassTransit).
- **+:** MongoDB + optimistic locking + ACID seletiva = consistência no ponto crítico sem custo global.
- **−:** disciplina para NÃO fazer query cross-coleção (teste deve falhar ao detectar).
- **−:** transações ACID só no finish (exige replica set).
