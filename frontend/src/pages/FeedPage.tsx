import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { ListingCard } from "../components/ListingCard";
import { ApiError, api } from "../api/client";
import type { Category, FeedItem, Kind } from "../api/types";
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
  const [categories, setCategories] = useState<Category[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [filter, setFilter] = useState<Filter>("Todos");
  const [categoriaId, setCategoriaId] = useState<string>("");
  const [query, setQuery] = useState("");
  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(true);

  useEffect(() => {
    api
      .categories()
      .then(setCategories)
      .catch(() => {
        /* categorias opcionais para o filtro */
      });
  }, []);

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError(null);
    api
      .feed({
        page: 1,
        kind: filter === "Todos" ? undefined : filter,
        categoriaId: categoriaId || undefined,
      })
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
  }, [filter, categoriaId]);

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
        categoriaId: categoriaId || undefined,
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
    <div className="py-8">
      <div className="flex flex-col gap-4 mb-6">
        <div className="flex flex-col sm:flex-row sm:items-center gap-3">
          <h1 className="text-2xl sm:text-3xl font-bold text-cream">Feed</h1>
          {user?.verified && (
            <Link
              to="/listings/new"
              className="sm:ml-auto bg-brand text-ink font-semibold px-4 py-2 rounded-lg text-sm text-center"
            >
              + Anunciar
            </Link>
          )}
        </div>

        <input
          type="search"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="Buscar por título…"
          className="w-full bg-smoke text-cream rounded-lg border border-smoke focus:border-esmeralda px-4 py-2.5 outline-none"
        />

        <div className="flex flex-col sm:flex-row gap-3">
          <div
            role="group"
            aria-label="Filtrar por tipo"
            className="flex gap-1 bg-charcoal rounded-lg border border-smoke p-1 w-full sm:w-auto"
          >
            {FILTERS.map((f) => (
              <button
                key={f}
                onClick={() => setFilter(f)}
                className={`flex-1 sm:flex-none px-3 py-1.5 rounded-md text-sm font-medium transition ${
                  filter === f ? "bg-brand text-ink" : "text-silver hover:text-cream"
                }`}
              >
                {FILTER_LABEL[f]}
              </button>
            ))}
          </div>

          <div className="flex-1 sm:max-w-xs">
            <label className="block">
              <span className="sr-only">Filtrar por categoria</span>
              <select
                value={categoriaId}
                onChange={(e) => setCategoriaId(e.target.value)}
                className="w-full bg-smoke text-cream rounded-lg border border-smoke focus:border-esmeralda px-4 py-2.5 outline-none"
              >
                <option value="">Todas as categorias</option>
                {categories.map((c) => (
                  <option key={c.Id} value={c.Id}>
                    {c.Nome}
                  </option>
                ))}
              </select>
            </label>
          </div>
        </div>
      </div>

      {error && (
        <div className="bg-smoke border border-smoke text-silver rounded-xl p-4 text-sm mb-6">
          {error}
        </div>
      )}

      {loading ? (
        <div className="grid gap-4 grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 2xl:grid-cols-5">
          {Array.from({ length: 10 }).map((_, i) => (
            <div key={i} className="aspect-[3/4] bg-smoke rounded-xl animate-pulse" />
          ))}
        </div>
      ) : visible.length === 0 ? (
        <div className="bg-charcoal rounded-xl border border-smoke p-8 text-center">
          <p className="text-silver">
            {q ? "Nenhum resultado para a busca." : "Nenhum anúncio por aqui ainda."}
          </p>
          {!q && user?.verified && (
            <Link
              to="/listings/new"
              className="mt-4 inline-block bg-brand text-ink font-semibold px-5 py-2.5 rounded-xl"
            >
              Criar o primeiro anúncio
            </Link>
          )}
        </div>
      ) : (
        <>
          <div className="grid gap-4 grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 2xl:grid-cols-5">
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
