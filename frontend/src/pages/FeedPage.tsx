import { useEffect, useRef, useState, type ReactNode } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { EmptyState } from "../components/EmptyState";
import { ListingCard } from "../components/ListingCard";
import { ListingTimelineCard } from "../components/ListingTimelineCard";
import { OnboardingChecklist } from "../components/OnboardingChecklist";
import { PostCard } from "../components/PostCard";
import { ApiError, api } from "../api/client";
import type { Category, FeedItem, Kind, Mode, PublicPostItem } from "../api/types";
import { useAuth } from "../auth/AuthContext";
import { MODO_META } from "../lib/config";

const LISTINGS_PAGE = 24;
const POSTS_PAGE = 20; // tamanho de página do backend (GetPublicPostsQuery)

type KindFilter = "Todos" | Kind;
type ModeFilter = "Todos" | Mode;

const MODE_FILTERS: ModeFilter[] = ["Todos", "Trade", "Resell", "Donate", "Volunteer"];
const KIND_FILTERS: KindFilter[] = ["Todos", "Product", "Service"];
const KIND_LABEL: Record<KindFilter, string> = {
  Todos: "Todos",
  Product: "Produtos",
  Service: "Serviços",
};

const TIMELINE_GRID =
  "grid gap-4 md:grid-cols-2 xl:grid-cols-3 2xl:grid-cols-4 items-start";

// Ritmo temporal do feed: quebras visíveis entre novo e antigo (diretriz de
// infinite scroll — marcar onde começa o "mais velho" orienta o scroll).
function bucketOf(iso: string): string {
  const ageDays = (Date.now() - new Date(iso).getTime()) / 86_400_000;
  if (Number.isFinite(ageDays) && ageDays < 1) return "Novo hoje";
  if (Number.isFinite(ageDays) && ageDays < 7) return "Esta semana";
  return "Mais antigas";
}

// Timeline unificada: posts (GET /api/posts) e anúncios (GET /api/listings/feed)
// mesclados por data — cards parecidos, sem destoar.
type TimelineEntry =
  | { tipo: "post"; key: string; at: number; item: PublicPostItem }
  | { tipo: "listing"; key: string; at: number; item: FeedItem };

export function FeedPage() {
  const { user } = useAuth();
  // Busca vinda do header (?q=) — a GlobalSearch navega para cá com o termo.
  const [searchParams, setSearchParams] = useSearchParams();
  const q = searchParams.get("q")?.trim() || "";
  const [items, setItems] = useState<FeedItem[]>([]);
  const [posts, setPosts] = useState<PublicPostItem[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [modeFilter, setModeFilter] = useState<ModeFilter>("Todos");
  const [kindFilter, setKindFilter] = useState<KindFilter>("Todos");
  const [categoriaId, setCategoriaId] = useState<string>("");
  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(true);
  const [postsPage, setPostsPage] = useState(1);
  const [postsHasMore, setPostsHasMore] = useState(true);
  const [savedIds, setSavedIds] = useState<Set<string>>(new Set());

  // Filtro de anúncio ativo → a timeline vira só anúncios (posts não têm
  // modo/kind/categoria/termo — não faz sentido misturá-los filtrados).
  const listingsOnly =
    modeFilter !== "Todos" || kindFilter !== "Todos" || !!categoriaId || !!q;

  // Rail "Grátis hoje" (carrega 1×; falha em silêncio — rail sem dado some).
  const [freeRail, setFreeRail] = useState<FeedItem[] | null>(null);

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
    setFreeRail(null);
    api
      .feed({ donationOnly: true, page: 1 })
      .then((data) => {
        if (active) setFreeRail(data.slice(0, 8));
      })
      .catch(() => {
        if (active) setFreeRail([]);
      });
    return () => {
      active = false;
    };
  }, []);

  // Bootstrap dos anúncios já salvos (marca o 🔖 na timeline sem refetch).
  useEffect(() => {
    if (!user) {
      setSavedIds(new Set());
      return;
    }
    let active = true;
    api
      .savedListingIds()
      .then((ids) => {
        if (active) setSavedIds(new Set(ids));
      })
      .catch(() => {
        /* marcador opcional */
      });
    return () => {
      active = false;
    };
  }, [user]);

  // Anúncios — recarrega a página 1 quando os filtros mudam.
  useEffect(() => {
    let active = true;
    setLoading(true);
    setError(null);
    api
      .feed({
        page: 1,
        mode: modeFilter === "Todos" ? undefined : modeFilter,
        kind: kindFilter === "Todos" ? undefined : kindFilter,
        categoryId: categoriaId || undefined,
        q: q || undefined,
      })
      .then((data) => {
        if (!active) return;
        setItems(data);
        setPage(1);
        setHasMore(data.length >= LISTINGS_PAGE);
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
  }, [modeFilter, kindFilter, categoriaId, q]);

  // Posts públicos — independentes dos filtros de anúncio (carrega 1×).
  useEffect(() => {
    let active = true;
    api
      .publicPosts(1)
      .then((data) => {
        if (!active) return;
        setPosts(data);
        setPostsPage(1);
        setPostsHasMore(data.length >= POSTS_PAGE);
      })
      .catch(() => {
        /* posts são a metade social — em silêncio mostra só anúncios */
        if (active) setPosts([]);
      });
    return () => {
      active = false;
    };
  }, []);

  async function loadMore() {
    if (loadingMore || loading || !hasMoreTotal) return;
    setLoadingMore(true);
    try {
      // Sem filtro: avança as duas fontes que ainda têm página. Com filtro de
      // anúncio ativo, só os anúncios (a timeline está listings-only).
      if (!listingsOnly && hasMore) {
        const next = page + 1;
        const data = await api.feed({ page: next, q: q || undefined });
        setItems((prev) => [...prev, ...data]);
        setPage(next);
        setHasMore(data.length >= LISTINGS_PAGE);
      }
      if (!listingsOnly && postsHasMore) {
        const next = postsPage + 1;
        const data = await api.publicPosts(next);
        setPosts((prev) => [...prev, ...data]);
        setPostsPage(next);
        setPostsHasMore(data.length >= POSTS_PAGE);
      }
      if (listingsOnly && hasMore) {
        const next = page + 1;
        const data = await api.feed({
          page: next,
          mode: modeFilter === "Todos" ? undefined : modeFilter,
          kind: kindFilter === "Todos" ? undefined : kindFilter,
          categoryId: categoriaId || undefined,
          q: q || undefined,
        });
        setItems((prev) => [...prev, ...data]);
        setPage(next);
        setHasMore(data.length >= LISTINGS_PAGE);
      }
    } catch (e) {
      setError(e instanceof ApiError ? e.message : "Erro ao carregar mais.");
    } finally {
      setLoadingMore(false);
    }
  }

  const hasMoreTotal = listingsOnly ? hasMore : hasMore || postsHasMore;

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
  }, [hasMoreTotal]);

  // Mescla por CreatedAt desc; key "tipo-Id" (post e anúncio podem ter o mesmo guid).
  const timeline: TimelineEntry[] = [
    ...(listingsOnly ? [] : posts.map((p) => ({ tipo: "post" as const, key: `post-${p.Post.Id}`, at: new Date(p.Post.CreatedAt).getTime(), item: p }))),
    ...items.map((i) => ({
      tipo: "listing" as const,
      key: `listing-${i.Id}`,
      at: i.CreatedAt ? new Date(i.CreatedAt).getTime() : Number.NaN,
      item: i,
    })),
  ].sort((a, b) => b.at - a.at);

  // Seções temporais: quando muda o rótulo, muda a seção.
  const sections: { label: string; entries: TimelineEntry[] }[] = [];
  for (const e of timeline) {
    if (!Number.isFinite(e.at)) continue;
    const label = bucketOf(new Date(e.at).toISOString());
    const last = sections[sections.length - 1];
    if (last && last.label === label) last.entries.push(e);
    else sections.push({ label, entries: [e] });
  }

  return (
    <div className="app-container">
      <div className="flex flex-col gap-4 mb-6">
        <div className="flex flex-col sm:flex-row sm:items-center gap-3">
          <div>
            <h1 className="text-2xl sm:text-3xl font-bold text-cream">Feed</h1>
            <p className="text-sm text-silver">
              Conversas e anúncios da sua rede em uma linha do tempo
            </p>
          </div>
          {user && (
            <Link
              to="/saved"
              className="text-sm text-silver hover:text-amber sm:ml-2 sm:mt-1"
            >
              🔖 Salvos
            </Link>
          )}
          <Link
            to="/explore"
            className="text-sm text-esmeralda hover:underline sm:ml-2 sm:mt-1"
          >
            Buscar com filtros →
          </Link>
          {user?.verified && (
            <Link
              to="/listings/new"
              className="sm:ml-auto bg-brand text-ink font-semibold px-4 py-2 rounded-lg text-sm text-center"
            >
              + Anunciar
            </Link>
          )}
        </div>

        {/* Busca ativa (?q= do header) — chip removível */}
        {q && (
          <div className="flex items-center gap-2">
            <span className="inline-flex items-center gap-2 bg-smoke border border-smoke text-cream text-sm px-3 py-1.5 rounded-full">
              🔎 {q}
              <button
                type="button"
                onClick={() => setSearchParams({}, { replace: true })}
                aria-label="Limpar busca"
                className="text-silver hover:text-rosa leading-none"
              >
                ✕
              </button>
            </span>
            {!listingsOnly && (
              <span className="text-xs text-silver">mostrando só anúncios</span>
            )}
          </div>
        )}

        {/* Modo — o diferencial semântico do revoa, cor por papel (§4/§6) */}
        <div className="flex gap-2 overflow-x-auto pb-1 -mx-1 px-1">
          {MODE_FILTERS.map((m) => {
            const active = modeFilter === m;
            const meta = m === "Todos" ? null : MODO_META[m];
            return (
              <button
                key={m}
                onClick={() => setModeFilter(m)}
                className={`px-3.5 py-1.5 rounded-full text-sm font-medium border whitespace-nowrap transition ${
                  active
                    ? meta
                      ? `${meta.bg} ${meta.text} border-transparent`
                      : "bg-brand text-ink border-transparent"
                    : "border-smoke text-silver hover:text-cream"
                }`}
              >
                {meta ? `${meta.emoji} ${meta.label}` : "Tudo"}
              </button>
            );
          })}
        </div>

        <div className="flex flex-col sm:flex-row gap-3">
          <div
            role="group"
            aria-label="Filtrar por tipo"
            className="flex gap-1 bg-charcoal rounded-lg border border-smoke p-1 w-full sm:w-auto"
          >
            {KIND_FILTERS.map((f) => (
              <button
                key={f}
                onClick={() => setKindFilter(f)}
                className={`flex-1 sm:flex-none px-3 py-1.5 rounded-md text-sm font-medium transition ${
                  kindFilter === f ? "bg-brand text-ink" : "text-silver hover:text-cream"
                }`}
              >
                {KIND_LABEL[f]}
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
                    {c.Name}
                  </option>
                ))}
              </select>
            </label>
          </div>
        </div>
      </div>

      {/* Conta nova — checklist das primeiras ações (some ao completar) */}
      <OnboardingChecklist />

      {error && (
        <div className="bg-smoke border border-smoke text-silver rounded-xl p-4 text-sm mb-6">
          {error}
        </div>
      )}

      {loading ? (
        <div className={TIMELINE_GRID}>
          {Array.from({ length: 8 }).map((_, i) => (
            <div key={i} className="h-32 bg-smoke rounded-xl animate-pulse" />
          ))}
        </div>
      ) : timeline.length === 0 ? (
        q ? (
          <EmptyState
            icon="🔎"
            title={`Nada encontrado para “${q}”.`}
            action={
              <button
                type="button"
                onClick={() => setSearchParams({}, { replace: true })}
                className="inline-block bg-brand text-ink font-semibold px-5 py-2.5 rounded-xl"
              >
                Limpar busca
              </button>
            }
          />
        ) : (
          <EmptyState
            icon="♻️"
            title="Nada por aqui ainda."
            action={
              user?.verified ? (
                <Link
                  to="/listings/new"
                  className="inline-block bg-brand text-ink font-semibold px-5 py-2.5 rounded-xl"
                >
                  Criar o primeiro anúncio
                </Link>
              ) : undefined
            }
          />
        )
      ) : (
        <>
          {/* Rail grátis só sem filtro ativo — com filtro duplicaria o resultado */}
          {!listingsOnly && freeRail && freeRail.length > 0 && (
            <Rail title="Grátis hoje" seeAllTo="/explore">
              {freeRail.map((it) => (
                <div key={it.Id} className="w-56 shrink-0 snap-start">
                  <ListingCard item={it} />
                </div>
              ))}
            </Rail>
          )}

          {sections.map((s) => (
            <section key={s.label} className="mb-8">
              <h2 className="text-xs font-bold uppercase tracking-wider text-silver mb-3">
                {s.label}
              </h2>
              <div className={TIMELINE_GRID}>
                {s.entries.map((e) =>
                  e.tipo === "post" ? (
                    <PostCard
                      key={e.key}
                      post={e.item.Post}
                      communityName={e.item.CommunityName}
                      communityUrl={`/community/${e.item.Post.CommunityId}#conversas`}
                      currentUserId={user?.userId}
                    />
                  ) : (
                    <ListingTimelineCard
                      key={e.key}
                      item={e.item}
                      currentUserId={user?.userId}
                      initialSaved={savedIds.has(e.item.Id)}
                    />
                  )
                )}
              </div>
            </section>
          ))}

          {hasMoreTotal && (
            <div ref={sentinelRef} className="text-center mt-8">
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

function Rail({
  title,
  seeAllTo,
  children,
}: {
  title: string;
  seeAllTo?: string;
  children: ReactNode;
}) {
  return (
    <section className="mb-8">
      <div className="flex items-baseline justify-between gap-3 mb-3">
        <h2 className="text-xs font-bold uppercase tracking-wider text-silver">{title}</h2>
        {seeAllTo && (
          <Link to={seeAllTo} className="text-sm text-esmeralda hover:underline">
            ver tudo →
          </Link>
        )}
      </div>
      <div className="flex gap-4 overflow-x-auto pb-2 snap-x">{children}</div>
    </section>
  );
}
