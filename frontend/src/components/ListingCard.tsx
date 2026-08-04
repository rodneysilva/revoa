import { Link } from "react-router-dom";
import { Badge } from "./Badge";
import { KIND_LABELS } from "../lib/config";
import type { FeedItem } from "../api/types";

export function ListingCard({ item }: { item: FeedItem }) {
  const gratis = item.PrecoRvm === 0;
  return (
    <Link
      to={`/listings/${item.Id}`}
      className="group block bg-charcoal rounded-xl border border-smoke overflow-hidden hover:border-esmeralda/60 transition"
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
          <span aria-hidden>📦</span>
        )}
      </div>
      <div className="p-4">
        <div className="flex items-center gap-2 mb-2">
          <Badge modo={item.Modo} />
          <span className="text-xs text-silver">{KIND_LABELS[item.Kind]}</span>
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
        <div className="mt-3 flex items-center gap-2 text-xs text-silver">
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
      </div>
    </Link>
  );
}
