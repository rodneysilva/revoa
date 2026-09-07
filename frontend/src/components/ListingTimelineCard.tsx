import { useState } from "react";
import { Link } from "react-router-dom";
import { Avatar } from "./Avatar";
import { Badge } from "./Badge";
import { ShareButton } from "./ShareButton";
import { brlEstimate } from "../lib/format";
import { useBrlRate } from "../lib/useBrlRate";
import { api } from "../api/client";
import type { FeedItem } from "../api/types";

// Anúncio na timeline unificada do feed: a mesma moldura do PostCard
// (bg-charcoal border-smoke rounded-xl + action bar) com o conteúdo próprio
// do anúncio — thumb, modo, título, preço e CTA para o detalhe.
export function ListingTimelineCard({
  item,
  currentUserId,
  initialSaved,
}: {
  item: FeedItem;
  currentUserId?: string;
  initialSaved?: boolean;
}) {
  const [saved, setSaved] = useState(initialSaved ?? false);
  const { rate } = useBrlRate();

  const gratis = item.PriceRvm === 0;
  const brl = !gratis ? brlEstimate(item.PriceRvm, rate ?? 0) : null;
  const local =
    item.DistanciaKm != null
      ? `~${item.DistanciaKm.toFixed(1)} km`
      : item.Neighborhood || item.City || null;
  const detailUrl = `/listings/${item.Id}`;

  async function toggleSave() {
    if (!currentUserId) return;
    const next = !saved;
    setSaved(next);
    try {
      const on = await api.saveListing(item.Id);
      if (on !== next) setSaved(on);
    } catch {
      setSaved(!next);
    }
  }

  return (
    <article className="rounded-xl bg-charcoal border border-smoke p-4">
      <div className="flex gap-4">
        <Link
          to={detailUrl}
          className="w-24 h-24 shrink-0 rounded-lg overflow-hidden bg-smoke flex items-center justify-center text-3xl"
        >
          {item.PrimeiraImagem ? (
            <img
              src={item.PrimeiraImagem}
              alt={item.Title}
              className="w-full h-full object-cover"
              loading="lazy"
              onError={(e) => {
                (e.target as HTMLImageElement).style.display = "none";
              }}
            />
          ) : (
            <span aria-hidden>{item.Kind === "Service" ? "🛠️" : "📦"}</span>
          )}
        </Link>

        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 flex-wrap">
            <Badge modo={item.Mode} />
            <span className="ml-auto text-xs text-silver whitespace-nowrap">
              {local ? `📍 ${local}` : "📍"}
            </span>
          </div>

          <Link to={detailUrl} className="block mt-1.5 group">
            <h3 className="font-semibold text-cream line-clamp-2 group-hover:text-esmeralda">
              {item.Title}
            </h3>
          </Link>

          <div className="mt-1">
            {gratis ? (
              <span className="rms text-lima">Grátis · RM$ 0</span>
            ) : (
              <span className="rms text-cream">
                RM$ {item.PriceRvm.toLocaleString("pt-BR")}
                {brl && <span className="text-xs text-silver ml-1.5">≈ {brl}</span>}
              </span>
            )}
          </div>

          <div className="mt-2 flex items-center gap-2 text-xs text-silver">
            <Avatar name={item.SellerName} src={item.SellerAvatarUrl} size={20} />
            <span className="truncate">{item.SellerName}</span>
          </div>

          <div className="mt-2.5 flex items-center gap-4">
            {currentUserId ? (
              <button
                type="button"
                onClick={toggleSave}
                className={`text-xs hover:text-amber transition ${saved ? "text-amber" : "text-silver"}`}
              >
                {saved ? "🔖 Salvo" : "🔖 Salvar"}
              </button>
            ) : (
              <Link to="/login" className="text-xs text-silver hover:text-amber">
                🔖 Salvar
              </Link>
            )}
            <ShareButton
              url={`${window.location.origin}${detailUrl}`}
              label=""
              className="text-xs text-silver hover:text-esmeralda"
            />
            <Link
              to={detailUrl}
              className="ml-auto bg-brand text-ink text-xs font-semibold px-3 py-1.5 rounded-lg"
            >
              Ver anúncio
            </Link>
          </div>
        </div>
      </div>
    </article>
  );
}
