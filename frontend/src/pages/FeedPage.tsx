import { useEffect, useRef, useState, type ReactNode } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { Avatar } from "../components/Avatar";
import { EmptyState } from "../components/EmptyState";
import { ListingCard, ListingCardSkeleton } from "../components/ListingCard";
import { ApiError, api } from "../api/client";
import type { Category, Community, FeedItem, Kind, Mode, Post } from "../api/types";
import { useAuth } from "../auth/AuthContext";
import { timeAgo } from "../lib/time";
import { MODO_META } from "../lib/config";

const PAGE_SIZE = 24;

type KindFilter = "Todos" | Kind;
type ModeFilter = "Todos" | Mode;

const MODE_FILTERS: ModeFilter[] = ["Todos", "Trade", "Resell", "Donate", "Volunteer"];
const KIND_FILTERS: KindFilter[] = ["Todos", "Product", "Service"];
const KIND_LABEL: Record<KindFilter, string> = {
  Todos: "Todos",
  Product: "Produtos",
  Service: "Serviços",
};

const GRID_CLASS =
  "grid gap-4 grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 2xl:grid-cols-5 3xl:grid-cols-6";

// Ritmo temporal do feed: quebras visíveis entre novo e antigo (diretriz de
// infinite scroll — marcar onde começa o "mais velho" orienta o scroll).
function bucketOf(iso: string): string {
  const ageDays = (Date.now() - new Date(iso).getTime()) / 86_400_000;
  if (Number.isFinite(ageDays) && ageDays < 1) return "Novo hoje";
  if (Number.isFinite(ageDays) && ageDays < 7) return "Esta semana";
  return "Mais antigas";
}

type RailPost = { post: Post; community: Community };

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
  const [kindFilter, setKindFilter] = useState<KindFilter>("Todos");
  const [categoriaId, setCategoriaId] = useState<string>("");
  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(true);

  // Rails (carregam 1×; falham em silêncio — rail que não tem dado some).
  const [freeRail, setFreeRail] = useState<FeedItem[] | null>(null);
  const [postsRail, setPostsRail] = useState<RailPost[] | null>(null);

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

  // "Da sua comunidade": batimento social das comunidades do usuário, resolvido
  // no servidor (mine/posts) — raízes e respostas recentes em uma chamada.
  useEffect(() => {
    if (!user) {
      setPostsRail(null);
      return;
    }
    let active = true;
    api
      .socialFeed()
      .then((items) => {
        if (active)
          setPostsRail(
            items.slice(0, 4).map((i) => ({ post: i.Post, community: i.Community }))
          );
      })
      .catch(() => {
        if (active) setPostsRail([]);
      });
    return () => {
      active = false;
    };
  }, [user]);

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
  }, [modeFilter, kindFilter, categoriaId, q]);

  async function loadMore() {
    if (loadingMore || !hasMore || loading) return;
    setLoadingMore(true);
    const next = page + 1;
    try {
      const data = await api.feed({
        page: next,
        mode: modeFilter === "Todos" ? undefined : modeFilter,
        kind: kindFilter === "Todos" ? undefined : kindFilter,
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

  const semFiltro =
    modeFilter === "Todos" && kindFilter === "Todos" && !categoriaId && !q;

  // Seções temporais: o feed chega CreatedAt desc — muda o rótulo, muda a seção.
  const sections: { label: string; items: FeedItem[] }[] = [];
  for (const it of items) {
    if (!it.CreatedAt) continue;
    const label = bucketOf(it.CreatedAt);
    const last = sections[sections.length - 1];
    if (last && last.label === label) last.items.push(it);
    else sections.push({ label, items: [it] });
  }

  return (
    <div className="app-container">
      <div className="flex flex-col gap-4 mb-6">
        <div className="flex flex-col sm:flex-row sm:items-center gap-3">
          <div>
            <h1 className="text-2xl sm:text-3xl font-bold text-cream">Feed</h1>
            <p className="text-sm text-silver">O que está acontecendo na sua rede</p>
          </div>
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

      {error && (
        <div className="bg-smoke border border-smoke text-silver rounded-xl p-4 text-sm mb-6">
          {error}
        </div>
      )}

      {loading ? (
        <div className={GRID_CLASS}>
          {Array.from({ length: 10 }).map((_, i) => (
            <ListingCardSkeleton key={i} />
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
                className="inline-block bg-brand text-ink font-semibold px-5 py-2.5 rounded-xl"
              >
                Limpar busca
              </button>
            }
          />
        ) : (
          <EmptyState
            icon="♻️"
            title="Nenhum anúncio por aqui ainda."
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
          {/* Rails só sem filtro ativo — com filtro eles duplicariam o resultado */}
          {semFiltro && postsRail && postsRail.length > 0 && (
            <Rail title="Da sua comunidade" seeAllTo="/community">
              {postsRail.map((r) => (
                <PostRailCard key={r.post.Id} post={r.post} community={r.community} />
              ))}
            </Rail>
          )}

          {semFiltro && freeRail && freeRail.length > 0 && (
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
              <div className={GRID_CLASS}>
                {s.items.map((it) => (
                  <ListingCard key={it.Id} item={it} />
                ))}
              </div>
            </section>
          ))}

          {hasMore && (
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

function PostRailCard({ post, community }: RailPost) {
  return (
    <Link
      to={`/community/${community.Id}#conversas`}
      className="w-72 shrink-0 snap-start bg-charcoal border border-smoke rounded-xl p-4 hover:border-amber/60 transition"
    >
      <div className="flex items-center gap-2 text-xs text-silver mb-2">
        <Avatar name={post.AuthorName} src={post.AutorAvatarUrl} size={22} />
        <span className="truncate font-medium text-cream">{post.AuthorName}</span>
        <span className="ml-auto whitespace-nowrap">{timeAgo(post.CreatedAt)}</span>
      </div>
      <p className="text-sm text-cream/90 line-clamp-3 whitespace-pre-wrap">{post.Content}</p>
      <p className="mt-2 text-xs text-amber">💬 {community.Name}</p>
    </Link>
  );
}
