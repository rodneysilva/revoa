// Configuração central do SPA revoa.me.
// O Vite faz proxy de /api e /hubs para o backend .NET (http://localhost:8000),
// então usamos paths relativos — API_BASE fica vazio.
export const API_BASE = "";

// Os 8 slogans da hero (docs/VISUAL_IDENTITY.md §2) — rotativos.
export const SLOGANS = [
  "Tudo encontra um novo lar.",
  "Dá nova vida ao que você tem.",
  "Comunidade que troca, doa e cuida.",
  "Ofereça o que sabe. Receba o que precisa.",
  "Mais comunidade, menos desperdício.",
  "Onde a ajuda tem asas.",
  "Ajuda mútua, de vizinho para vizinho.",
  "O que você não usa encontra quem precisa.",
];

export const KIND_LABELS = {
  Product: "Produto",
  Service: "Serviço",
} as const;

// Metadados por modo (emoji + classes Tailwind literais p/ o scanner gerar).
// Cores por modo (docs/VISUAL_IDENTITY.md §6):
// Trocar=esmeralda · Repassar=rosa · Doar=terracota · Voluntariar=lima.
// `label` = forma verbal (CTAs, filtros); `badge` = forma do badge (§6, tabela
// de badges) — Repassar vira "Acessível" na UI de anúncio (UF-08).
export const MODO_META = {
  Trade: {
    emoji: "🔄",
    label: "Trocar",
    badge: "Troca",
    desc: "Troca direta de produto ou serviço entre vizinhos.",
    text: "text-esmeralda",
    bg: "bg-esmeralda/15",
  },
  Resell: {
    emoji: "💜",
    label: "Repassar",
    badge: "Acessível",
    desc: "Repassa algo que você intermediou para a comunidade.",
    text: "text-rosa",
    bg: "bg-rosa/15",
  },
  Donate: {
    emoji: "🎁",
    label: "Doar",
    badge: "Doação",
    desc: "Dá de graça a quem precisa (produto).",
    text: "text-terracota",
    bg: "bg-terracota/15",
  },
  Volunteer: {
    emoji: "🤝",
    label: "Voluntariar",
    badge: "Voluntário",
    desc: "Ofereça seu tempo e habilidades (serviço).",
    text: "text-lima",
    bg: "bg-lima/15",
  },
} as const;

export const ALL_MODOS = ["Trade", "Resell", "Donate", "Volunteer"] as const;

// Regras Modo × Kind (docs/BUSINESS_RULES.md):
// Product: Trocar, Repassar, Doar  ·  Service: Trocar, Voluntariar.
export function modosForKind(kind: "Product" | "Service") {
  return ALL_MODOS.filter((m) =>
    kind === "Product"
      ? m === "Trade" || m === "Resell" || m === "Donate"
      : m === "Trade" || m === "Volunteer"
  );
}
