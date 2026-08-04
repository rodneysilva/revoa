import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { ListingCard } from "../components/ListingCard";
import { ApiError, api } from "../api/client";
import type { FeedItem, Kind } from "../api/types";
import { useAuth } from "../auth/AuthContext";

type Filter = "Todos" | Kind;

const FILTERS: Filter[] = ["Todos", "Product", "Service"];
const FILTER_LABEL: Record<Filter, string> = {
  Todos: "Todos",
  Product: "Produtos",
  Service: "Serviços",
};

export function FeedPage() {
  const { user } = useAuth();
  const [items, setItems] = useState<FeedItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [filter, setFilter] = useState<Filter>("Todos");
  const [query, setQuery] = useState("");
  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(true);

  // Recarrega quando o filtro muda.
  useEffect(() => {
    let active = true;
    setLoading(true);
    setError(null);
    api
      .feed({ page: 1, kind: filter === "Todos" ? undefined : filter })
      .then((data) => {
        if (!active) return;
        setItems(data);
        setPage(1);
        setHasMore(data.length > 0);
      })
      .catch((e) => {
        if (active)
          setError(e instanceof ApiError ? e.message : "Erro ao carregar o feed.");
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, [filter]);

  const q = query.trim().toLowerCase();
  const visible = q
    ? items.filter((it) => it.Titulo.toLowerCase().includes(q))
    : items;

  async function loadMore() {
    if (loadingMore || !hasMore || q) return;
    setLoadingMore(true);
    const next = page + 1;
    try {
      const data = await api.feed({
        page: next,
        kind: filter === "Todos" ? undefined : filter,
      });
      setItems((prev) => [...prev, ...data]);
      setPage(next);
      setHasMore(data.length > 0);
    } catch (e) {
      setError(e instanceof ApiError ? e.message : "Erro ao carregar mais.");
    } finally {
      setLoadingMore(false);
    }
  }

  return (
    <div className="mx-auto max-w-6xl px-4 py-8">
      <div className="flex flex-col sm:flex-row sm:items-center gap-4 mb-6">
        <h1 className="text-2xl font-bold text-cream">Feed</h1>
        <input
          type="search"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="Buscar por título…"
          className="sm:ml-4 flex-1 bg-smoke text-cream rounded-lg border border-smoke focus:border-esmeralda px-4 py-2 outline-none"
        />
        <div className="flex gap-1 bg-charcoal rounded-lg border border-smoke p-1">
          {FILTERS.map((f) => (
            <button
              key={f}
              onClick={() => setFilter(f)}
              className={`px-3 py-1.5 rounded-md text-sm font-medium transition ${
                filter === f ? "bg-brand text-ink" : "text-silver hover:text-cream"
              }`}
            >
              {FILTER_LABEL[f]}
            </button>
          ))}
        </div>
        {user?.verified && (
          <Link
            to="/listings/new"
            className="bg-brand text-ink font-semibold px-4 py-2 rounded-lg text-sm"
          >
            + Anunciar
          </Link>
        )}
      </div>

      {error && (
        <div className="bg-smoke border border-smoke text-silver rounded-xl p-4 text-sm mb-6">
          {error}
        </div>
      )}

      {loading ? (
        <div className="grid gap-4 grid-cols-2 sm:grid-cols-3 lg:grid-cols-4">
          {Array.from({ length: 8 }).map((_, i) => (
            <div key={i} className="aspect-[3/4] bg-smoke rounded-xl animate-pulse" />
          ))}
        </div>
      ) : visible.length === 0 ? (
        <p className="text-silver">
          {q ? "Nenhum resultado para a busca." : "Nenhum anúncio por aqui ainda."}
        </p>
      ) : (
        <>
          <div className="grid gap-4 grid-cols-2 sm:grid-cols-3 lg:grid-cols-4">
            {visible.map((it) => (
              <ListingCard key={it.Id} item={it} />
            ))}
          </div>
          {!q && hasMore && (
            <div className="text-center mt-8">
              <button
                onClick={loadMore}
                disabled={loadingMore}
                className="border border-smoke text-cream font-semibold px-6 py-2.5 rounded-xl hover:border-esmeralda disabled:opacity-60"
              >
                {loadingMore ? "Carregando…" : "Carregar mais"}
              </button>
            </div>
          )}
        </>
      )}
    </div>
  );
}
