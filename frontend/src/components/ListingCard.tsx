import { Link } from "react-router-dom";
import { Package, Wrench } from "lucide-react";
import { Avatar } from "./Avatar";
import { Badge } from "./Badge";
import { brlEstimate } from "../lib/format";
import { useBrlRate } from "../lib/useBrlRate";
import type { FeedItem } from "../api/types";

// Card de anúncio (VISUAL_IDENTITY §8): imagem + badge por modo + título +
// "Grátis · RM$ 0" ou preço com referência BRL + vendedor com local.
// Condição/duração ficam na página de detalhe — no grid são ruído.

// Placeholder de carregamento com a mesma forma do card real (imagem quadrada +
// bloco de texto) — o layout não "pula" quando os dados chegam.
export function ListingCardSkeleton() {
  return (
    <div className="bg-charcoal rounded-xl border border-smoke overflow-hidden animate-pulse">
      <div className="aspect-[4/5] bg-smoke" />
      <div className="p-4">
        <div className="h-3.5 w-16 rounded-full bg-smoke mb-2.5" />
        <div className="h-4 w-2/3 rounded bg-smoke mb-2" />
        <div className="h-5 w-1/3 rounded bg-smoke mb-3" />
        <div className="h-3 w-1/2 rounded bg-smoke" />
      </div>
    </div>
  );
}

export function ListingCard({ item }: { item: FeedItem }) {
  const gratis = item.PriceRvm === 0;
  const { rate } = useBrlRate();
  const brl = !gratis ? brlEstimate(item.PriceRvm, rate ?? 0) : null;
  const local =
    item.DistanciaKm != null
      ? `~${item.DistanciaKm.toFixed(1)} km`
      : item.Neighborhood || item.City || null;

  return (
    <Link
      to={`/listings/${item.Id}`}
      className="group flex flex-col bg-charcoal rounded-xl border border-smoke overflow-hidden hover:border-esmeralda/60 transition"
    >
      <div className="aspect-[4/5] bg-smoke flex items-center justify-center">
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
          <span aria-hidden className="text-silver">
            {item.Kind === "Service" ? (
              <Wrench className="w-10 h-10" />
            ) : (
              <Package className="w-10 h-10" />
            )}
          </span>
        )}
      </div>
      <div className="p-4 flex flex-col flex-1">
        <Badge modo={item.Mode} />

        <h3 className="mt-2 font-semibold text-cream line-clamp-2 group-hover:text-esmeralda">
          {item.Title}
        </h3>

        <div className="mt-1.5">
          {gratis ? (
            <span className="rms text-lima">Grátis · RM$ 0</span>
          ) : (
            <div className="flex flex-col gap-0.5">
              <span className="rms text-cream">
                RM$ {item.PriceRvm.toLocaleString("pt-BR")}
              </span>
              {brl && (
                <span className="text-xs text-silver">≈ {brl}</span>
              )}
            </div>
          )}
        </div>

        <div className="mt-auto pt-3 flex items-center gap-2 text-xs text-silver">
          <Avatar name={item.SellerName} src={item.SellerAvatarUrl} size={20} />
          <span className="truncate">{item.SellerName}</span>
          {local && <span className="ml-auto whitespace-nowrap truncate">{local}</span>}
        </div>
      </div>
    </Link>
  );
}
