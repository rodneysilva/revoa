import { Link } from "react-router-dom";
import { Badge } from "./Badge";
import { timeAgo } from "../lib/time";
import type { FeedItem } from "../api/types";

const CONDITION_LABEL: Record<string, string> = {
  Novo: "Novo",
  Seminovo: "Seminovo",
  Usado: "Usado",
};

function serviceDurationLabel(item: FeedItem): string | null {
  if (item.Kind !== "Service" || item.Duration == null) return null;
  return item.UnitType === "Hours" ? `≈ ${item.Duration} h` : "por serviço";
}

export function ListingCard({ item }: { item: FeedItem }) {
  const gratis = item.PrecoRvm === 0;
  const isService = item.Kind === "Service";
  const condition = !isService && item.Condition
    ? CONDITION_LABEL[item.Condition] ?? item.Condition
    : null;
  const duration = serviceDurationLabel(item);
  const when = item.CreatedAt ? timeAgo(item.CreatedAt) : null;

  return (
    <Link
      to={`/listings/${item.Id}`}
      className="group flex flex-col bg-charcoal rounded-xl border border-smoke overflow-hidden hover:border-esmeralda/60 transition"
    >
      <div className="aspect-square bg-smoke flex items-center justify-center text-5xl">
        {item.PrimeiraImagem ? (
          <img
            src={item.PrimeiraImagem}
            alt={item.Titulo}
            className="w-full h-full object-cover"
            loading="lazy"
          />
        ) : (
          <span aria-hidden>{isService ? "🛠️" : "📦"}</span>
        )}
      </div>
      <div className="p-4 flex flex-col flex-1">
        <div className="flex items-center gap-1.5 mb-2 flex-wrap">
          <Badge modo={item.Modo} />
          {isService ? (
            <span className="text-xs px-2 py-0.5 rounded-full bg-sky/15 text-sky font-medium">
              serviço
            </span>
          ) : condition ? (
            <span className="text-xs px-2 py-0.5 rounded-full bg-smoke text-silver font-medium">
              {condition}
            </span>
          ) : null}
          {duration && (
            <span className="text-xs px-2 py-0.5 rounded-full bg-smoke text-silver font-medium">
              ⏱ {duration}
            </span>
          )}
        </div>

        <h3 className="font-semibold text-cream line-clamp-1 group-hover:text-esmeralda">
          {item.Titulo}
        </h3>

        <div className="mt-1">
          {gratis ? (
            <span className="rms text-lima">Grátis</span>
          ) : (
            <span className="rms text-cream">
              RM$ {item.PrecoRvm.toLocaleString("pt-BR")}
            </span>
          )}
        </div>

        <div className="mt-auto pt-3 flex items-center gap-2 text-xs text-silver">
          {item.VendedorAvatarUrl ? (
            <img
              src={item.VendedorAvatarUrl}
              alt=""
              className="w-5 h-5 rounded-full"
            />
          ) : (
            <span className="w-5 h-5 rounded-full bg-charcoal border border-smoke" />
          )}
          <span className="truncate">{item.VendedorNome}</span>
          {item.DistanciaKm != null ? (
            <span className="ml-auto whitespace-nowrap">~{item.DistanciaKm.toFixed(1)} km</span>
          ) : item.Cidade ? (
            <span className="ml-auto truncate">{item.Cidade}</span>
          ) : null}
        </div>

        {when && (
          <div className="mt-1.5 text-xs text-silver/70">{when}</div>
        )}
      </div>
    </Link>
  );
}
