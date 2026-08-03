# ADR-0011: Dois domínios — revoa.me (app) e revoa.org (transparência)

## Status
Accepted — decisão v3.0.

## Contexto
O projeto tem duas zonas Cloudflare: `revoa.me` e `revoa.org`. O `.me` hospeda o app/produto. O `.org`
(usp/convenção de "organização sem fins lucrativos") reforça o caráter social e é o lugar natural para
missão, transparência e impacto. Precisávamos definir a relação entre os dois para deploy, roteamento e SEO.

## Decisão
- **`revoa.me`** = **app/plataforma** de troca/doação (o produto). Roteado para `revoa-app:8000`.
- **`revoa.org`** = **blog + documentação pública + painel de transparência/impacto** (missão, prestação
  de contas do Fundo Comunitário, métricas de impacto — itens doados, resíduo evitado, etc.).
- Implementação: o `.org` pode ser **(a) rota dedicada no mesmo app** (`revoa.org/transparencia`) servindo
  conteúdo estático/blog, **ou (b) site institucional separado** (estático, na Fase 4). Decisão de
  implementação deferida; ambas as rotas já passam pelo Traefik (routers-revoa.yml aponta ambos para o app).

## Alternativas consideradas
- **Ambos = mesmo app (redirect .org→.me):** desperdiça o `.org` como ativo de marca/confiança.
- **.org = internacional, .me = Brasil:** escopo futuro; não alinhado ao MVP nacional.

## Consequências
- **+:** `.org` reforça "sem fins lucrativos" + hospeda transparência (confiança + captação futura).
- **+:** SEO separado (produto vs. institucional/missão).
- **−:** manter conteúdo em dois "lugares" (mitigado: blog/transparência pode ser estático/CMS leve).
