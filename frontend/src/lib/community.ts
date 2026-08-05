import type { EixoComunidade, PapelMembro, VisibilidadeComunidade } from "../api/types";

export const EIXOS: EixoComunidade[] = ["Geo", "Interesse", "Causa"];
export const EIXO_FILTERS = ["Todos", "Geo", "Interesse", "Causa"] as const;
export const EIXO_LABEL: Record<EixoComunidade, string> = {
  Geo: "Geográfica",
  Interesse: "Interesse",
  Causa: "Causa",
};
export const EIXO_EMOJI: Record<EixoComunidade, string> = {
  Geo: "📍",
  Interesse: "💡",
  Causa: "🤲",
};
export const VISIBILIDADES: VisibilidadeComunidade[] = ["Open", "Private"];
export const VISIBILIDADE_LABEL: Record<VisibilidadeComunidade, string> = {
  Open: "Aberta",
  Private: "Privada",
};
export const PAPEL_META: Record<PapelMembro, { label: string; cls: string }> = {
  Criador: { label: "Criador", cls: "bg-amber/15 text-amber" },
  Moderador: { label: "Moderador", cls: "bg-sky/15 text-sky" },
  Membro: { label: "Membro", cls: "bg-smoke text-silver" },
};
