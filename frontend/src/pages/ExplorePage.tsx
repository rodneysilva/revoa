import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { ListingCard } from "../components/ListingCard";
import { ApiError, api } from "../api/client";
import type { Category, FeedItem, FeedParams, Kind, Modo } from "../api/types";
import { useAuth } from "../auth/AuthContext";

const PAGE_SIZE = 24;

type KindFilter = "Todos" | Kind;
type ModoFilter = "Todos" | Modo;
type SortKey = "recente" | "preco-asc" | "preco-desc";

const KIND_OPTIONS: KindFilter[] = ["Todos", "Product", "Service"];
const KIND_LABEL: Record<KindFilter, string> = {
  Todos: "Todos",
  Product: "Produtos",
  Service: "Serviços",
};
const MODO_OPTIONS: ModoFilter[] = ["Todos", "Trocar", "Repassar", "Doar", "Voluntariar"];
const SORT_OPTIONS: { value: SortKey; label: string }[] = [
  { value: "recente", label: "Mais recentes" },
  { value: "preco-asc", label: "Preço ↑" },
  { value: "preco-desc", label: "Preço ↓" },
];

const inputCls =
  "w-full bg-smoke text-cream rounded-lg border border-smoke focus:border-esmeralda px-3 py-2 outline-none text-sm";
const labelCls = "block text-xs font-medium text-silver mb-1.5";

export function ExplorePage() {
  const { user } = useAuth();

  const [q, setQ] = useState("");
  const [debouncedQ, setDebouncedQ] = useState("");
  const [kind, setKind] = useState<KindFilter>("Todos");
  const [modo, setModo] = useState<ModoFilter>("Todos");
  const [categoriaId, setCategoriaId] = useState("");
  const [precoMin, setPrecoMin] = useState("");
  const [precoMax, setPrecoMax] = useState("");
  const [doarApenas, setDoarApenas] = useState(false);
  const [sort, setSort] = useState<SortKey>("recente");

  const [categories, setCategories] = useState<Category[]>([]);
  const [items, setItems] = useState<FeedItem[]>([]);
  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(true);
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [filtersOpen, setFiltersOpen] = useState(false);

  useEffect(() => {
    api.categories().then(setCategories).catch(() => {});
  }, []);

  useEffect(() => {
    const t = setTimeout(() => setDebouncedQ(q.trim()), 350);
    return () => clearTimeout(t);
  }, [q]);

  const buildParams = useMemo(
    () =>
      (p: number): FeedParams => ({
        page: p,
        q: debouncedQ || undefined,
        kind: kind === "Todos" ? undefined : kind,
        modo: modo === "Todos" ? undefined : modo,
        categoriaId: categoriaId || undefined,
        precoMin: precoMin ? Number(precoMin) : undefined,
        precoMax: precoMax ? Number(precoMax) : undefined,
        doarApenas: doarApenas || undefined,
        sort,
      }),
    [debouncedQ, kind, modo, categoriaId, precoMin, precoMax, doarApenas, sort]
  );

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError(null);
    api
      .feed(buildParams(1))
      .then((data) => {
        if (!active) return;
        setItems(data);
        setPage(1);
        setHasMore(data.length >= PAGE_SIZE);
      })
      .catch((e) => {
        if (active)
          setError(e instanceof ApiError ? e.message : "Erro ao buscar anúncios.");
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, [buildParams]);

  async function loadMore() {
    if (loadingMore || !hasMore) return;
    setLoadingMore(true);
    const next = page + 1;
    try {
      const data = await api.feed(buildParams(next));
      setItems((prev) => [...prev, ...data]);
      setPage(next);
      setHasMore(data.length >= PAGE_SIZE);
    } catch (e) {
      setError(e instanceof ApiError ? e.message : "Erro ao carregar mais.");
    } finally {
      setLoadingMore(false);
    }
  }

  function clearFilters() {
    setQ("");
    setDebouncedQ("");
    setKind("Todos");
    setModo("Todos");
    setCategoriaId("");
    setPrecoMin("");
    setPrecoMax("");
    setDoarApenas(false);
    setSort("recente");
  }

  const activeCount =
    (kind !== "Todos" ? 1 : 0) +
    (modo !== "Todos" ? 1 : 0) +
    (categoriaId ? 1 : 0) +
    (precoMin || precoMax ? 1 : 0) +
    (doarApenas ? 1 : 0) +
    (sort !== "recente" ? 1 : 0) +
    (debouncedQ ? 1 : 0);

  const Filters = (
    <div className="bg-charcoal rounded-xl border border-smoke p-4 space-y-4">
      <div>
        <label className="block">
          <span className="sr-only">Buscar</span>
          <input
            type="search"
            value={q}
            onChange={(e) => setQ(e.target.value)}
            placeholder="Buscar por título ou descrição…"
            className={inputCls}
          />
        </label>
      </div>

      <div>
        <span className={labelCls}>Tipo</span>
        <div className="flex gap-1 bg-smoke rounded-lg border border-smoke p-1">
          {KIND_OPTIONS.map((k) => (
            <button
              key={k}
              onClick={() => setKind(k)}
              className={`flex-1 px-2 py-1.5 rounded-md text-sm font-medium transition ${
                kind === k ? "bg-brand text-ink" : "text-silver hover:text-cream"
              }`}
            >
              {KIND_LABEL[k]}
            </button>
          ))}
        </div>
      </div>

      <div>
        <span className={labelCls}>Modo</span>
        <div className="flex flex-wrap gap-1 bg-smoke rounded-lg border border-smoke p-1">
          {MODO_OPTIONS.map((m) => (
            <button
              key={m}
              onClick={() => setModo(m)}
              className={`flex-1 min-w-[5.5rem] px-2 py-1.5 rounded-md text-sm font-medium transition ${
                modo === m ? "bg-brand text-ink" : "text-silver hover:text-cream"
              }`}
            >
              {m === "Todos" ? "Todos" : m}
            </button>
          ))}
        </div>
      </div>

      <div>
        <label className="block">
          <span className={labelCls}>Categoria</span>
          <select
            value={categoriaId}
            onChange={(e) => setCategoriaId(e.target.value)}
            className={inputCls}
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

      <div>
        <span className={labelCls}>Faixa de preço (RM$)</span>
        <div className="flex items-center gap-2">
          <input
            type="number"
            min={0}
            inputMode="numeric"
            value={precoMin}
            onChange={(e) => setPrecoMin(e.target.value)}
            placeholder="mín"
            className={inputCls}
            aria-label="Preço mínimo"
          />
          <span className="text-silver">—</span>
          <input
            type="number"
            min={0}
            inputMode="numeric"
            value={precoMax}
            onChange={(e) => setPrecoMax(e.target.value)}
            placeholder="máx"
            className={inputCls}
            aria-label="Preço máximo"
          />
        </div>
      </div>

      <label className="flex items-center gap-2 cursor-pointer select-none">
        <input
          type="checkbox"
          checked={doarApenas}
          onChange={(e) => setDoarApenas(e.target.checked)}
          className="w-4 h-4 accent-esmeralda"
        />
        <span className="text-sm text-cream">Só grátis (doações e voluntariado)</span>
      </label>

      <div>
        <label className="block">
          <span className={labelCls}>Ordenar por</span>
          <select
            value={sort}
            onChange={(e) => setSort(e.target.value as SortKey)}
            className={inputCls}
          >
            {SORT_OPTIONS.map((s) => (
              <option key={s.value} value={s.value}>
                {s.label}
              </option>
            ))}
          </select>
        </label>
      </div>

      <div className="pt-1 border-t border-smoke">
        <span className="block text-xs text-silver/70 pt-3">
          📍 Filtro por proximidade (raio) — em breve
        </span>
      </div>

      {activeCount > 0 && (
        <button
          onClick={clearFilters}
          className="w-full text-sm text-esmeralda hover:underline pt-1"
        >
          Limpar filtros ({activeCount})
        </button>
      )}
    </div>
  );

  return (
    <div className="app-container">
      <div className="flex flex-col sm:flex-row sm:items-center gap-3 mb-6">
        <h1 className="text-2xl sm:text-3xl font-bold text-cream">Explorar</h1>
        <span className="text-sm text-silver sm:ml-1">
          {loading ? "Buscando…" : `${items.length} anúncio${items.length === 1 ? "" : "s"}`}
        </span>
        {user?.verified && (
          <Link
            to="/listings/new"
            className="sm:ml-auto bg-brand text-ink font-semibold px-4 py-2 rounded-lg text-sm text-center"
          >
            + Anunciar
          </Link>
        )}
      </div>

      <div className="grid gap-6 lg:grid-cols-[280px_1fr]">
        <aside className="hidden lg:block">
          <div className="sticky top-20">{Filters}</div>
        </aside>

        <div>
          <button
            onClick={() => setFiltersOpen((o) => !o)}
            className="lg:hidden w-full mb-4 flex items-center justify-between bg-charcoal rounded-lg border border-smoke px-4 py-2.5 text-sm font-medium text-cream"
          >
            <span>Filtros{activeCount > 0 ? ` (${activeCount})` : ""}</span>
            <span aria-hidden>{filtersOpen ? "✕" : "☰"}</span>
          </button>
          {filtersOpen && <div className="lg:hidden mb-6">{Filters}</div>}

          {error && (
            <div className="bg-smoke border border-smoke text-silver rounded-xl p-4 text-sm mb-6">
              {error}
            </div>
          )}

          {loading ? (
            <div className="grid gap-4 grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 2xl:grid-cols-5 3xl:grid-cols-6">
              {Array.from({ length: 10 }).map((_, i) => (
                <div
                  key={i}
                  className="aspect-[3/4] bg-smoke rounded-xl animate-pulse"
                />
              ))}
            </div>
          ) : items.length === 0 ? (
            <div className="bg-charcoal rounded-xl border border-smoke p-8 text-center">
              <p className="text-silver mb-1">Nenhum anúncio com esses filtros.</p>
              <p className="text-sm text-silver/70 mb-4">
                Tente ajustar a busca ou limpar os filtros.
              </p>
              {activeCount > 0 && (
                <button
                  onClick={clearFilters}
                  className="inline-block bg-brand text-ink font-semibold px-5 py-2.5 rounded-xl"
                >
                  Limpar filtros
                </button>
              )}
              {!activeCount && user?.verified && (
                <Link
                  to="/listings/new"
                  className="inline-block bg-brand text-ink font-semibold px-5 py-2.5 rounded-xl"
                >
                  Criar o primeiro anúncio
                </Link>
              )}
            </div>
          ) : (
            <>
              <div className="grid gap-4 grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 2xl:grid-cols-5 3xl:grid-cols-6">
                {items.map((it) => (
                  <ListingCard key={it.Id} item={it} />
                ))}
              </div>
              {hasMore && (
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
      </div>
    </div>
  );
}
