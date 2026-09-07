import { Link } from "react-router-dom";
import { Avatar } from "./Avatar";
import { timeAgo } from "../lib/time";
import { MODO_META } from "../lib/config";
import type { FeedItem } from "../api/types";

// Card de publicação de anúncio (padrão "anúncio de membro" do feed social):
// autor com avatar + chip da categoria, capa à esquerda, fatos do anúncio,
// preço em RM$ e CTA de conversa com o anunciante. Usado no feed e na aba
// Anúncios da comunidade.
export function PublicationCard({
  item,
  categoryName,
}: {
  item: FeedItem;
  categoryName?: string;
}) {
  const modo = MODO_META[item.Mode];
  const local = [
    item.Neighborhood || item.City,
    item.DistanciaKm != null ? `${item.DistanciaKm.toFixed(0)} km` : null,
  ]
    .filter(Boolean)
    .join(", ");
  const primeiroNome = item.SellerName.split(" ")[0];

  return (
    <article className="overflow-hidden rounded-2xl border border-smoke bg-charcoal">
      <div className="grid grid-cols-[minmax(0,1fr)_auto] items-start gap-3 p-4 pb-3">
        <div className="flex min-w-0 items-center gap-3">
          <Avatar name={item.SellerName} src={item.SellerAvatarUrl} size={40} />
          <div className="min-w-0">
            <p className="truncate text-sm font-semibold text-cream">
              {item.SellerName}
            </p>
            <p className="truncate text-xs text-silver">
              Anúncio{item.CreatedAt ? ` · ${timeAgo(item.CreatedAt)}` : ""}
            </p>
          </div>
        </div>
        {categoryName && (
          <span className="inline-flex shrink-0 items-center gap-1 rounded-full bg-smoke px-2.5 py-1 text-[11px] font-semibold text-cream">
            🏷️ {categoryName}
          </span>
        )}
      </div>

      <div className="grid gap-4 px-4 pb-4 sm:grid-cols-[minmax(0,180px)_minmax(0,1fr)]">
        <Link
          to={`/listings/${item.Id}`}
          className="block h-44 w-full overflow-hidden rounded-xl bg-smoke"
        >
          {item.PrimeiraImagem ? (
            <img
              src={item.PrimeiraImagem}
              alt=""
              loading="lazy"
              className="h-full w-full object-cover"
            />
          ) : (
            <span
              aria-hidden
              className="grid h-full w-full place-items-center text-4xl"
            >
              {item.Kind === "Service" ? "🛠️" : "📦"}
            </span>
          )}
        </Link>

        <div className="flex min-w-0 flex-col justify-between gap-3">
          <div className="min-w-0">
            <h3 className="text-base font-semibold leading-snug text-cream">
              <Link to={`/listings/${item.Id}`} className="hover:text-esmeralda">
                {item.Title}
              </Link>
            </h3>
            <p className="mt-1 flex flex-wrap items-center gap-x-2 gap-y-1 text-xs text-silver">
              {item.Condition && <span>{item.Condition}</span>}
              {item.Condition && local && <span aria-hidden>·</span>}
              {local && <span className="inline-flex items-center gap-1">📍 {local}</span>}
            </p>
          </div>

          <div className="grid grid-cols-[minmax(0,1fr)_auto] items-center gap-3">
            <p className="min-w-0 text-lg font-semibold">
              {item.PriceRvm === 0 ? (
                <span className="text-lima">Grátis</span>
              ) : (
                <span className="rms">
                  {modo.emoji} RM$ {item.PriceRvm.toLocaleString("pt-BR")}
                </span>
              )}
            </p>
            <Link
              to={`/listings/${item.Id}`}
              className="inline-flex shrink-0 items-center gap-1.5 rounded-full bg-brand px-4 py-2 text-sm font-medium text-ink transition-opacity hover:opacity-90"
            >
              💬 Falar com {primeiroNome}
            </Link>
          </div>
        </div>
      </div>
    </article>
  );
}
