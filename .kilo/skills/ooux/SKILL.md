---
name: ooux
description: >-
  Design objects-first (OOUX) com o processo ORCA. Use em TODA feature nova do
  projeto revoa (e em qualquer produto onde a confusão entre "telas/fluxos" e
  "objetos da mente do usuário" gere retrabalho). Modela os OBJETOS que o usuário
  pensa (pessoas, coisas, lugares, conceitos) ANTES de telas, fluxos ou actions.
  Cada objeto OOUX → um aggregate DDD. Anti-padrões: actions-first, page-first.
metadata:
  category: methodology
  source: internal
---

# Skill OOUX — Object-Oriented UX (processo ORCA)

> **OOUX = design objects-first.** Os usuários pensam em **objetos** (pessoas, lugares, coisas,
> conceitos), não em "telas" ou "fluxos". Antes de desenhar UMA tela, modele os objetos da mente do
> usuário. Depois conecte-os, dê ações (CTAs) e atributos. Só então desenhe views/states/fluxos.
>
> **No revoa:** toda feature nova começa pelo mapa de objetos em [`docs/OOUX.md`](../../../docs/OOUX.md).
> Cada objeto OOUX vira um **aggregate DDD** (alinhamento arquitetural). YAGNI: só modele objetos com
> **CTA/estado reais** — objeto sem ação concreta é ruído.

---

## Quando usar

- **TODA feature nova** do revoa, antes de qualquer wireframe/fluxo.
- Ao chegar um requisito em linguagem de **ação** ("o usuário cadastra/compra/posta") → traduza para **objetos** primeiro.
- Ao revisar um design que "parece uma tela" mas ninguém nomeou os objetos — retroceda para OOUX.
- Ao planejar o domínio DDD: o mapa OOUX é a semente dos aggregates.

## Quando NÃO usar

- Ajustes cosméticos (cor, copy, espaçamento).
- Refatoração pura de código sem mudança de modelo mental.
- Bug fix pontual.

---

## O processo ORCA (4 passos)

### Passo 1 — Objects (extrair substantivos)
Pegue os **objetivos** do usuário (jobs-to-be-done) e **destaque os substantivos**.
Cada substantivo relevante = candidato a objeto.

Perguntas-chave:
- Com o que o usuário interage? (pessoa, lugar, coisa, conceito)
- Sobre o que o usuário quer ver informações? (= objeto)
- Sobre o que o usuário age? (= CTA)

Filtre: mantenha só objetos que aparecem em **múltiplos** objetivos ou que têm **estado/CTA** próprio.
Objeto mencionado uma vez, sem estado = atributo de outro objeto (não vira objeto).

> Exemplo revoa: objetivo *"ofereço o que sei fazer e recebo RVM"* → objetos: **Usuário**, **Anúncio (service)**, **RVM**, **Carteira**.

### Passo 2 — Relationships (como os objetos se conectam)
Para cada par de objetos, defina a cardinalidade e a navegação:
- 1:N, N:M, 1:1.
- **Navegação contextual:** qual objeto "contém" ou "leva a" qual? (ex.: Anúncio → vendedor (Usuário); Comunidade → Posts; Carteira → Transações).

Desenhe a **matriz de relacionamentos** (cross-check: cada relação aparece nos dois sentidos?).

### Passo 3 — CTAs (ações por objeto)
Para cada objeto, liste o que o usuário **faz com ele**:
- Criar, editar, excluir, visualizar, **transferir** (self-custody!), avaliar, denunciar, resgatar (cupom), reivindicar (claim).

**Regra de ouro:** objeto sem NENHUMA CTA é atributo, não objeto. (YAGNI aplicado ao OOUX.)

### Passo 4 — Attributes + Views + States
- **Attributes (core/metadata):** dados essenciais do objeto. Separe **core** (identidade) de **metadata** (auxiliar, pode vir depois).
- **Views:** em quais contextos o objeto aparece? (card no feed, detalhe, perfil, admin, notificação).
- **States:** o objeto tem ciclo de vida? (ex.: Anúncio `draft→active→in_progress→sold|cancelled`; Troca `offered→funded→delivered→released|disputed`).
- **Ranking forçado:** ordene atributos/CTAs por prioridade — o mais importante primeiro. Força decisões (evita "tudo é importante").

---

## Anti-padrões (NUNCA)

- **Actions-first:** começar pelo fluxo ("cadastro → login → feed → comprar") antes de nomear os objetos.
- **Page-first:** começar pelo "desenho da tela" sem saber quais objetos ela exibe/manipula.
- **Objeto fantasma:** objeto sem CTA nem estado (vira atributo ou é excluído).
- **Atributo-objeto confusão:** tratar um atributo como objeto (ex.: "endereço" é atributo de Usuário, não objeto — salvo se tiver ciclo de vida próprio).
- **Navegação implícita:** relação entre objetos sem direção definida.

---

## Saída padrão (atualizar `docs/OOUX.md`)

Para cada objeto, registre:
```
### <Objeto>
- Atributos core: ...
- Atributos metadata: ...
- CTAs: ...
- Relacionamentos: → <outro> (cardinalidade)
- Views: feed | detalhe | perfil | ...
- States: estadoA → estadoB → ...
- Aggregate DDD: <Module>.<Aggregate>
- Ranking (1..n): ...
```

---

## OOUX → DDD (alinhamento revoa)

| OOUX | DDD |
|------|-----|
| Objeto | Aggregate root |
| Atributos core | Value objects / entidades do aggregate |
| CTAs | Commands (CQRS) / métodos do aggregate |
| States | Máquina de estados do aggregate |
| Views | Read models (DTOs) |
| Relacionamentos | Referências por ID (nunca navegação acoplada entre módulos — isolamento) |

> **Isolamento de módulos:** um objeto de um bounded context referencia outro só por **ID** (e via eventos).
> Nunca query cross-coleção. O OOUX deixa essa fronteira explícita.

---

## Referências

- Sophia Prater, *Object-Oriented UX* — [ooux.com](https://ooux.com)
- "Designing the Way People Think", A List Apart — [alistapart.com/article/object-oriented-ux](https://alistapart.com/article/object-oriented-ux/)
- Mapa-fonte do revoa: [`docs/OOUX.md`](../../../docs/OOUX.md)

---

## Checklist rápido (cole no início de toda feature)

- [ ] Nomeei os **objetos** (substantivos dos objetivos)?
- [ ] Defini **relacionamentos** com cardinalidade e navegação?
- [ ] Cada objeto tem ao menos uma **CTA**? (senão → atributo/excluir)
- [ ] Listei **atributos core vs metadata**, com **ranking forçado**?
- [ ] Defini **views** e **states** (se houver ciclo de vida)?
- [ ] Mapeei cada objeto a um **aggregate DDD** respeitando **isolamento** de módulo?
