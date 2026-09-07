import { useEffect, useMemo, useRef, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { EmptyState } from "../components/EmptyState";
import { PublicationCard } from "../components/PublicationCard";
import { ApiError, api } from "../api/client";
import type { Category, FeedItem, Mode } from "../api/types";
import { useAuth } from "../auth/AuthContext";
import { MODO_META } from "../lib/config";

const PAGE_SIZE = 24;

type ModeFilter = "Todos" | Mode;

const MODE_FILTERS: ModeFilter[] = ["Todos", "Trade", "Resell", "Donate", "Volunteer"];

/* Feed de publicações: coluna única de anúncios (card de publicação) com
   pílulas de categoria — modo é o filtro semântico do revoa (§4/§6). */
export function FeedPage() {
  const { user } = useAuth();
  // Busca vinda do header (?q=) — a GlobalSearch navega para cá com o termo.
  const [searchParams, setSearchParams] = useSearchParams();
  const q = searchParams.get("q")?.trim() || "";
  const [items, setItems] = useState<FeedItem[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [modeFilter, setModeFilter] = useState<ModeFilter>("Todos");
  const [categoriaId, setCategoriaId] = useState<string>("");
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
        mode: modeFilter === "Todos" ? undefined : modeFilter,
        categoryId: categoriaId || undefined,
        q: q || undefined,
      })
      .then((data) => {
        if (!active) return;
        setItems(data);
        setPage(1);
        setHasMore(data.length >= PAGE_SIZE);
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
  }, [modeFilter, categoriaId, q]);

  async function loadMore() {
    if (loadingMore || !hasMore || loading) return;
    setLoadingMore(true);
    const next = page + 1;
    try {
      const data = await api.feed({
        page: next,
        mode: modeFilter === "Todos" ? undefined : modeFilter,
        categoryId: categoriaId || undefined,
        q: q || undefined,
      });
      setItems((prev) => [...prev, ...data]);
      setPage(next);
      setHasMore(data.length >= PAGE_SIZE);
    } catch (e) {
      setError(e instanceof ApiError ? e.message : "Erro ao carregar mais.");
    } finally {
      setLoadingMore(false);
    }
  }

  // Infinite scroll: sentinela dispara o load 600px antes do fim; o botão
  // "Carregar mais" continua como fallback.
  const loadMoreRef = useRef(loadMore);
  loadMoreRef.current = loadMore;
  const sentinelRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    const el = sentinelRef.current;
    if (!el) return;
    const obs = new IntersectionObserver(
      (entries) => {
        if (entries.some((e) => e.isIntersecting)) loadMoreRef.current();
      },
      { rootMargin: "600px" }
    );
    obs.observe(el);
    return () => obs.disconnect();
  }, [hasMore]);

  const categoryById = useMemo(
    () => new Map(categories.map((c) => [c.Id, c.Name])),
    [categories]
  );

  return (
    <div className="app-container">
      <div className="mb-6 flex flex-col gap-4 sm:flex-row sm:items-center">
        <div>
          <h1 className="text-2xl font-bold text-cream sm:text-3xl">Feed</h1>
          <p className="text-sm text-silver">Publicações da sua rede</p>
        </div>
        {user?.verified && (
          <Link
            to="/listings/new"
            className="rounded-full bg-brand px-5 py-2 text-center text-sm font-semibold text-ink transition-opacity hover:opacity-90 sm:ml-auto"
          >
            + Anunciar
          </Link>
        )}
      </div>

      {/* Busca ativa (?q= do header) — chip removível */}
      {q && (
        <div className="mb-4 flex items-center gap-2">
          <span className="inline-flex items-center gap-2 rounded-full border border-smoke bg-smoke px-3 py-1.5 text-sm text-cream">
            🔎 {q}
            <button
              type="button"
              onClick={() => setSearchParams({}, { replace: true })}
              aria-label="Limpar busca"
              className="leading-none text-silver hover:text-rosa"
            >
              ✕
            </button>
          </span>
        </div>
      )}

      {/* Modo — o diferencial semântico do revoa, cor por papel (§4/§6) */}
      <div className="-mx-1 flex gap-2 overflow-x-auto px-1 pb-1">
        {MODE_FILTERS.map((m) => {
          const active = modeFilter === m;
          const meta = m === "Todos" ? null : MODO_META[m];
          return (
            <button
              key={m}
              onClick={() => setModeFilter(m)}
              className={`whitespace-nowrap rounded-full border px-3.5 py-1.5 text-sm font-medium transition ${
                active
                  ? meta
                    ? `${meta.bg} ${meta.text} border-transparent`
                    : "border-transparent bg-brand text-ink"
                  : "border-smoke text-silver hover:text-cream"
              }`}
            >
              {meta ? `${meta.emoji} ${meta.label}` : "Tudo"}
            </button>
          );
        })}
      </div>

      {/* Categorias — pílulas do feed */}
      {categories.length > 0 && (
        <nav aria-label="Categorias" className="-mx-4 mt-3 overflow-x-auto px-4 sm:mx-0 sm:px-0">
          <ul className="flex w-max gap-2 sm:w-auto sm:flex-wrap">
            <li>
              <button
                type="button"
                aria-pressed={categoriaId === ""}
                onClick={() => setCategoriaId("")}
                className={`whitespace-nowrap rounded-full border px-3.5 py-1.5 text-sm font-medium transition-colors ${
                  categoriaId === ""
                    ? "border-transparent bg-brand text-ink"
                    : "border-smoke text-silver hover:bg-smoke/60 hover:text-cream"
                }`}
              >
                Tudo
              </button>
            </li>
            {categories.map((c) => (
              <li key={c.Id}>
                <button
                  type="button"
                  aria-pressed={categoriaId === c.Id}
                  onClick={() => setCategoriaId(categoriaId === c.Id ? "" : c.Id)}
                  className={`whitespace-nowrap rounded-full border px-3.5 py-1.5 text-sm font-medium transition-colors ${
                    categoriaId === c.Id
                      ? "border-transparent bg-brand text-ink"
                      : "border-smoke text-silver hover:bg-smoke/60 hover:text-cream"
                  }`}
                >
                  {c.Name}
                </button>
              </li>
            ))}
          </ul>
        </nav>
      )}

      {error && (
        <div className="mb-6 mt-4 rounded-2xl border border-smoke bg-smoke/50 p-4 text-sm text-silver">
          {error}
        </div>
      )}

      <div className="mx-auto mt-5 max-w-2xl">
        {loading ? (
          <div className="space-y-4">
            {Array.from({ length: 4 }).map((_, i) => (
              <div
                key={i}
                className="h-52 animate-pulse rounded-2xl border border-smoke bg-smoke/50"
              />
            ))}
          </div>
        ) : items.length === 0 ? (
          q ? (
            <EmptyState
              icon="🔎"
              title={`Nenhum anúncio encontrado para “${q}”.`}
              action={
                <button
                  type="button"
                  onClick={() => setSearchParams({}, { replace: true })}
                  className="inline-block rounded-full bg-brand px-5 py-2.5 text-sm font-semibold text-ink"
                >
                  Limpar busca
                </button>
              }
            />
          ) : (
            <EmptyState
              icon="♻️"
              title="Nenhuma publicação por aqui ainda."
              action={
                user?.verified ? (
                  <Link
                    to="/listings/new"
                    className="inline-block rounded-full bg-brand px-5 py-2.5 text-sm font-semibold text-ink"
                  >
                    Criar o primeiro anúncio
                  </Link>
                ) : undefined
              }
            />
          )
        ) : (
          <>
            <div className="space-y-4">
              {items.map((it) => (
                <PublicationCard
                  key={it.Id}
                  item={it}
                  categoryName={categoryById.get(it.CategoryId)}
                />
              ))}
            </div>

            {hasMore && (
              <div ref={sentinelRef} className="mt-8 text-center">
                <button
                  onClick={loadMore}
                  disabled={loadingMore}
                  className="rounded-full border border-smoke px-6 py-2.5 font-semibold text-cream hover:border-esmeralda disabled:opacity-60"
                >
                  {loadingMore ? "Carregando…" : "Carregar mais"}
                </button>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  );
}
