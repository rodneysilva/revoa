import { useState } from "react";
import { Link } from "react-router-dom";
import { Avatar } from "./Avatar";
import { Badge } from "./Badge";
import { ShareButton } from "./ShareButton";
import { brlEstimate } from "../lib/format";
import { timeAgo } from "../lib/time";
import { useBrlRate } from "../lib/useBrlRate";
import { api } from "../api/client";
import type { FeedItem } from "../api/types";

// Anúncio na timeline unificada: a MESMA estrutura do PostCard (avatar + nome
// + tempo, conteúdo, action bar na moldura bg-charcoal border-smoke) com o
// conteúdo próprio do anúncio — foto em destaque, título, descrição e preço.
export function ListingTimelineCard({
  item,
  currentUserId,
  initialSaved,
  onUnsave,
}: {
  item: FeedItem;
  currentUserId?: string;
  initialSaved?: boolean;
  // "Meus salvos": avisa a página quando o anúncio deixa de estar salvo.
  onUnsave?: () => void;
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
      if (!on) onUnsave?.();
    } catch {
      setSaved(!next);
    }
  }

  return (
    <article className="rounded-xl bg-charcoal border border-smoke p-4 transition">
      {/* Cabeçalho — igual ao PostCard: vendedor + tempo + badge do modo */}
      <div className="flex gap-3">
        <Avatar name={item.SellerName} src={item.SellerAvatarUrl} size={40} />
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 flex-wrap">
            <span className="font-semibold text-cream truncate">{item.SellerName}</span>
            <span className="text-xs text-silver whitespace-nowrap">
              · {item.CreatedAt ? timeAgo(item.CreatedAt) : ""}
              {local ? ` · 📍 ${local}` : ""}
            </span>
            <span className="ml-auto shrink-0">
              <Badge modo={item.Mode} />
            </span>
          </div>

          {/* Foto em destaque (se houver) */}
          {item.PrimeiraImagem && (
            <Link
              to={detailUrl}
              className="block mt-2 rounded-lg overflow-hidden border border-smoke bg-smoke"
            >
              <img
                src={item.PrimeiraImagem}
                alt={item.Title}
                className="w-full aspect-[4/3] object-cover"
                loading="lazy"
                onError={(e) => {
                  (e.target as HTMLImageElement).style.display = "none";
                }}
              />
            </Link>
          )}

          <Link to={detailUrl} className="block mt-2 group">
            <h3 className="font-semibold text-cream line-clamp-1 group-hover:text-esmeralda">
              {item.Kind === "Service" ? "🛠️ " : "📦 "}
              {item.Title}
            </h3>
          </Link>

          {item.Description && (
            <p className="mt-1 text-sm text-cream/90 whitespace-pre-wrap break-words line-clamp-2">
              {item.Description}
            </p>
          )}

          <div className="mt-1.5">
            {gratis ? (
              <span className="rms text-lima">Grátis · RM$ 0</span>
            ) : (
              <span className="rms text-cream">
                RM$ {item.PriceRvm.toLocaleString("pt-BR")}
                {brl && <span className="text-xs text-silver ml-1.5">≈ {brl}</span>}
              </span>
            )}
          </div>

          {/* Action bar — mesma linha do PostCard */}
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
              className="ml-auto text-xs text-silver hover:text-esmeralda transition"
            >
              Ver anúncio →
            </Link>
          </div>
        </div>
      </div>
    </article>
  );
}
