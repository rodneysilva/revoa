# ADR-0006: Frontend React (Vite SPA) em vez de SolidJS

## Status
Accepted — decisão explícita do dono (Plano §1, decisão #17; handover).

## Contexto
O blueprint `equivale` usa **SolidJS** no frontend. O revoa poderia reusar esse conhecimento/stack.
Porém o dono decidiu **React**. A stack client (viem + permissionless.js + Safe SDK) é
framework-agnóstica, então a escolha de framework não trava a camada on-chain.

## Decisão
**React puro (Vite SPA) + Tailwind + viem + permissionless.js + Safe SDK + SignalR client + PWA.**
- Next.js/SSR só para marketing/SEO depois (não no MVP transacional).
- PWA instalável (manifest + service worker).
- Em produção, a SPA é servida pelo app .NET (host único).

## Alternativas consideradas
- **SolidJS (reusar equivale):** código frontend NÃO reusável de qualquer forma (UI/UX diferente);
  apenas padrões/carregam. Decisão do dono = React.
- **Next.js desde o início:** SSR traz complexidade (hidratação, AA client-side) sem benefício no
  app transacional; SEO é preocupação pós-MVP.

## Consequências
- **+:** ecossistema React (componentes, talento, docs) amplo; viem/permissionless/Safe SDK têm docs React.
- **−:** padrões/convenções do equivale (SolidJS) **não** se transferem como código — só como ideias.
- **−:** AA/signing é client-heavy (UserOps) — exige cuidado com state assíncrono no React.
