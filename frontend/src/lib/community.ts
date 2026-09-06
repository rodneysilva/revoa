import type { CommunityAxis, MembershipRole, CommunityVisibility } from "../api/types";

export const EIXOS: CommunityAxis[] = ["Geo", "Interest", "Cause"];
export const EIXO_FILTERS = ["Todos", "Geo", "Interest", "Cause"] as const;
export const EIXO_LABEL: Record<CommunityAxis, string> = {
  Geo: "Geográfica",
  Interest: "Interesse",
  Cause: "Causa",
};
export const EIXO_EMOJI: Record<CommunityAxis, string> = {
  Geo: "📍",
  Interest: "💡",
  Cause: "🤲",
};
export const VISIBILIDADES: CommunityVisibility[] = ["Open", "Private"];
export const VISIBILIDADE_LABEL: Record<CommunityVisibility, string> = {
  Open: "Aberta",
  Private: "Privada",
};
export const PAPEL_META: Record<MembershipRole, { label: string; cls: string }> = {
  Creator: { label: "Criador", cls: "bg-amber/15 text-amber" },
  Moderator: { label: "Moderador", cls: "bg-sky/15 text-sky" },
  Member: { label: "Membro", cls: "bg-smoke text-silver" },
};
