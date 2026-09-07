import { Link } from "react-router-dom";
import { Lock, MapPin, Users } from "lucide-react";
import { EIXO_ICON, EIXO_LABEL, PAPEL_META } from "../lib/community";
import type { Community, MembershipRole } from "../api/types";

// Card de comunidade (compartilhado entre a lista pública e "Minhas comunidades").
// `role` (opcional) marca o vínculo de quem está vendo — Criador/Moderador.
export function CommunityCard({ c, role }: { c: Community; role?: MembershipRole }) {
  const local = [c.Neighborhood, c.City, c.State].filter(Boolean).join(", ");
  const AxisIcon = EIXO_ICON[c.Axis];
  return (
    <Link
      to={`/community/${c.Id}`}
      className="group block bg-charcoal rounded-2xl border border-smoke overflow-hidden hover:border-amber/60 hover:shadow-lg transition"
    >
      <div className="bg-community relative h-[13.5rem] sm:h-[15rem] flex items-center px-4 overflow-hidden">
        {c.CoverImageUrl && (
          <img
            src={c.CoverImageUrl}
            alt=""
            loading="lazy"
            className="absolute inset-0 w-full h-full object-cover"
          />
        )}
        <span className="relative text-white drop-shadow" aria-hidden>
          <AxisIcon className="w-7 h-7" />
        </span>
        <div className="relative ml-auto flex items-center gap-1.5">
          {c.Type === "Default" && (
            <span className="text-[10px] font-semibold px-2 py-0.5 rounded-full bg-white/25 backdrop-blur-sm text-white">
              Oficial
            </span>
          )}
          {c.Visibility === "Private" && (
            <span className="text-white/90 px-1.5 py-0.5 rounded-full bg-black/30 backdrop-blur-sm">
              <Lock aria-hidden className="w-3 h-3" />
            </span>
          )}
        </div>
      </div>
      <div className="p-4">
        <div className="flex items-center gap-1.5 flex-wrap">
          <span className="text-xs font-semibold px-2 py-0.5 rounded-full bg-amber/15 text-amber">
            {EIXO_LABEL[c.Axis]}
          </span>
          {role && role !== "Member" && (
            <span className={`text-[10px] font-semibold px-2 py-0.5 rounded-full ${PAPEL_META[role].cls}`}>
              {PAPEL_META[role].label}
            </span>
          )}
        </div>
        <h3 className="mt-2 font-semibold text-cream group-hover:text-amber line-clamp-1">
          {c.Name}
        </h3>
        <p className="mt-1 text-sm text-silver line-clamp-2 min-h-[2.5rem]">
          {c.Description || "Sem descrição."}
        </p>
        <div className="mt-3 flex items-center gap-2 text-xs text-silver">
          <span className="inline-flex items-center gap-1">
            <Users aria-hidden className="w-3.5 h-3.5" />
            {c.MembersCount} {c.MembersCount === 1 ? "membro" : "membros"}
          </span>
          {local && (
            <span className="inline-flex items-center gap-1 truncate">
              · <MapPin aria-hidden className="w-3 h-3 shrink-0" />
              {local}
            </span>
          )}
        </div>
      </div>
    </Link>
  );
}
