import { useCallback, useEffect, useMemo, useState, type FormEvent } from "react";
import { Link, useLocation, useNavigate, useParams } from "react-router-dom";
import { ApiError, api } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import { Avatar } from "../components/Avatar";
import { EmptyState } from "../components/EmptyState";
import { LiveChat } from "../components/LiveChat";
import { PostThread } from "../components/PostThread";
import { PublicationCard } from "../components/PublicationCard";
import { timeAgo } from "../lib/time";
import {
  EIXO_EMOJI,
  EIXO_LABEL,
  PAPEL_META,
  VISIBILIDADE_LABEL,
} from "../lib/community";
import type {
  Category,
  Community,
  FeedItem,
  Membership,
  MembershipRole,
  Post,
} from "../api/types";

/* ═══════════════════════════════════════════════════════════════════════
   Navegação por objetos (OOUX — objects-first) no layout de comunidade:
   hero com capa, trilha lateral de objetos + categorias, painel central e
   coluna de membros/regras. Deep-link via #hash.
   Objetos: Conversas (Post) · Ao vivo (Chat) · Anúncios (Anúncio) · Membros (Membership).
   ═══════════════════════════════════════════════════════════════════════ */
type ObjectTab = "conversas" | "aovivo" | "ofertas" | "membros";

const TABS: { id: ObjectTab; icon: string; label: string }[] = [
  { id: "conversas", icon: "💬", label: "Conversas" },
  { id: "aovivo", icon: "⚡", label: "Ao vivo" },
  { id: "ofertas", icon: "🛍️", label: "Anúncios" },
  { id: "membros", icon: "👥", label: "Membros" },
];

function parseTab(hash: string): ObjectTab {
  const h = hash.replace(/^#/, "");
  return TABS.some((t) => t.id === h) ? (h as ObjectTab) : "conversas";
}

export function CommunityDetailPage() {
  const { id } = useParams<{ id: string }>();
  const location = useLocation();
  const navigate = useNavigate();
  const { user } = useAuth();
  const [community, setCommunity] = useState<Community | null>(null);
  const [posts, setPosts] = useState<Post[]>([]);
  const [members, setMembers] = useState<Membership[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [loading, setLoading] = useState(true);
  const [rootsLoading, setRootsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [joining, setJoining] = useState(false);
  const [leaving, setLeaving] = useState(false);
  const [joinPassword, setJoinPassword] = useState("");
  const [newPost, setNewPost] = useState("");
  const [posting, setPosting] = useState(false);
  const [replyingId, setReplyingId] = useState<string | null>(null);
  const [memberListings, setMemberListings] = useState<FeedItem[] | null>(null);
  const [listingsError, setListingsError] = useState(false);
  const [ofertaCat, setOfertaCat] = useState<string | null>(null);
  const [tab, setTab] = useState<ObjectTab>(() => parseTab(location.hash));

  useEffect(() => {
    setTab(parseTab(location.hash));
  }, [location.hash]);

  function selectTab(next: ObjectTab) {
    setTab(next);
    navigate({ hash: next }, { replace: true });
  }

  useEffect(() => {
    if (!id) return;
    let active = true;
    setLoading(true);
    setError(null);
    setRootsLoading(true);
    Promise.all([api.community(id), api.communityPosts(id), api.communityMembers(id)])
      .then(([c, p, m]) => {
        if (!active) return;
        setCommunity(c);
        setPosts(p);
        setMembers(m);
      })
      .catch((e) => {
        if (active) setError(e instanceof ApiError ? e.message : "Comunidade não encontrada.");
      })
      .finally(() => {
        if (active) {
          setLoading(false);
          setRootsLoading(false);
        }
      });
    return () => {
      active = false;
    };
  }, [id]);

  const activeMembers = useMemo(
    () => members.filter((m) => m.Status === "Active"),
    [members]
  );
  const isMember = !!(user && activeMembers.some((m) => m.UserId === user.userId));
  const isPrivate = community?.Visibility === "Private";

  useEffect(() => {
    api
      .categories()
      .then(setCategories)
      .catch(() => setCategories([]));
  }, []);

  useEffect(() => {
    const ids = Array.from(new Set(activeMembers.map((m) => m.UserId).filter(Boolean)));
    if (ids.length === 0) {
      setMemberListings([]);
      setListingsError(false);
      return;
    }
    let active = true;
    setMemberListings(null);
    setListingsError(false);
    // Anúncios dos membros VISÍVEIS no contexto desta comunidade: communityId
    // faz o backend incluir também os escopados à comunidade (Visibility=
    // Community), que o feed sem esse filtro exclui.
    api
      .feed({ sellerIds: ids.join(","), communityId: id, page: 1 })
      .then((items) => {
        if (active) setMemberListings(items);
      })
      .catch(() => {
        if (active) {
          setListingsError(true);
          setMemberListings([]);
        }
      });
    return () => {
      active = false;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [members]);

  // Categorias reais da comunidade = categorias dos anúncios dos membros.
  const communityCategories = useMemo(() => {
    const counts = new Map<string, number>();
    for (const l of memberListings ?? []) {
      counts.set(l.CategoryId, (counts.get(l.CategoryId) ?? 0) + 1);
    }
    const nameOf = (cid: string) => categories.find((c) => c.Id === cid)?.Name ?? "Outros";
    return Array.from(counts.entries())
      .map(([cid, n]) => ({ id: cid, name: nameOf(cid), n }))
      .sort((a, b) => b.n - a.n || a.name.localeCompare(b.name));
  }, [memberListings, categories]);

  const categoryById = useMemo(
    () => new Map(categories.map((c) => [c.Id, c.Name])),
    [categories]
  );

  const visibleListings = useMemo(
    () =>
      ofertaCat
        ? (memberListings ?? []).filter((l) => l.CategoryId === ofertaCat)
        : (memberListings ?? []),
    [memberListings, ofertaCat]
  );

  function pickCategory(cid: string | null) {
    setOfertaCat(cid);
    selectTab("ofertas");
  }

  async function join() {
    if (!id) return;
    setActionError(null);
    setJoining(true);
    try {
      await api.joinCommunity(id, joinPassword || undefined);
      const [m, c] = await Promise.all([api.communityMembers(id), api.community(id)]);
      setMembers(m);
      setCommunity(c);
      setJoinPassword("");
    } catch (e) {
      setActionError(e instanceof ApiError ? e.message : "Não foi possível entrar.");
    } finally {
      setJoining(false);
    }
  }

  async function leave() {
    if (!id) return;
    setActionError(null);
    setLeaving(true);
    try {
      await api.leaveCommunity(id);
      const [m, c] = await Promise.all([api.communityMembers(id), api.community(id)]);
      setMembers(m);
      setCommunity(c);
    } catch (e) {
      setActionError(e instanceof ApiError ? e.message : "Não foi possível sair.");
    } finally {
      setLeaving(false);
    }
  }

  async function submitRoot(e: FormEvent) {
    e.preventDefault();
    if (!id || !user) return;
    const c = newPost.trim();
    if (!c) return;
    setPosting(true);
    setActionError(null);
    try {
      const createdId = await api.createPost(id, undefined, c);
      const optimistic: Post = {
        Id: String(createdId),
        CommunityId: id,
        AutorId: user.userId,
        AuthorName: user.nome,
        AutorAvatarUrl: undefined,
        Content: c,
        Path: String(createdId),
        Depth: 0,
        Status: "Visible",
        CreatedAt: new Date().toISOString(),
      };
      setPosts((prev) => [optimistic, ...prev]);
      setNewPost("");
    } catch (e) {
      setActionError(e instanceof ApiError ? e.message : "Falha ao publicar.");
    } finally {
      setPosting(false);
    }
  }

  const handleReply = useCallback(
    async (parentId: string, conteudo: string) => {
      if (!id) return;
      setActionError(null);
      setReplyingId(parentId);
      try {
        await api.createPost(id, parentId, conteudo);
      } catch (e) {
        setActionError(e instanceof ApiError ? e.message : "Falha ao responder.");
      } finally {
        setReplyingId(null);
      }
    },
    [id]
  );

  const loadChildren = useCallback(
    async (parentId: string): Promise<Post[]> => {
      if (!id) return [];
      try {
        return await api.communityPosts(id, parentId);
      } catch {
        return [];
      }
    },
    [id]
  );

  if (loading) return <div className="app-container text-silver">Carregando comunidade…</div>;

  if (error || !community)
    return (
      <div className="app-container text-center">
        <p className="text-silver mb-4">{error ?? "Comunidade não encontrada."}</p>
        <Link to="/community" className="text-esmeralda hover:underline">
          ← Voltar às comunidades
        </Link>
      </div>
    );

  const local = [community.Neighborhood, community.City, community.State]
    .filter(Boolean)
    .join(", ");

  // Contadores apenas de objetos com dado real — "Ao vivo" não tem contador
  // (não há sinal de presença; dot pulsante seria fantasma).
  const counts: Partial<Record<ObjectTab, number>> = {
    conversas: posts.length,
    ofertas: memberListings?.length ?? 0,
    membros: activeMembers.length,
  };

  return (
    <div className="app-container">
      <Link
        to="/community"
        className="text-sm text-silver hover:text-cream mb-3 inline-block"
      >
        ← Comunidades
      </Link>

      {/* ══ HERO — capa em banda + nome + meta + CTA de participação ══ */}
      <section className="overflow-hidden rounded-3xl border border-smoke bg-charcoal">
        <div className="relative h-40 w-full sm:h-56">
          <div className="bg-community absolute inset-0" aria-hidden />
          <div className="absolute inset-0 bg-black/25" aria-hidden />
          <span
            aria-hidden
            className="absolute right-4 top-1/2 -translate-y-1/2 text-6xl opacity-40 sm:text-7xl"
          >
            {EIXO_EMOJI[community.Axis]}
          </span>
        </div>
        <div className="grid grid-cols-[minmax(0,1fr)_auto] items-center gap-4 p-4 sm:p-6">
          <div className="min-w-0">
            <h1 className="flex items-center gap-2 text-xl font-bold text-cream sm:text-2xl">
              <span className="truncate">{community.Name}</span>
              {community.Type === "Default" && (
                <span className="shrink-0 rounded-full bg-esmeralda/15 px-2 py-0.5 text-[10px] font-semibold text-esmeralda">
                  Oficial
                </span>
              )}
            </h1>
            <p className="mt-1 text-sm text-silver">
              Comunidade {VISIBILIDADE_LABEL[community.Visibility].toLowerCase()} ·{" "}
              {community.MembersCount} membros · {EIXO_LABEL[community.Axis]}
              {local && <> · 📍 {local}</>}
            </p>
            {community.Description && (
              <p className="mt-2 max-w-3xl text-sm text-silver line-clamp-2">
                {community.Description}
              </p>
            )}

            {/* Comunidade privada: senha para entrar */}
            {user?.verified && !isMember && isPrivate && (
              <div className="mt-3 flex flex-col gap-2 sm:flex-row sm:items-center">
                <input
                  type="password"
                  value={joinPassword}
                  onChange={(e) => setJoinPassword(e.target.value)}
                  placeholder="Senha da comunidade"
                  className="rounded-full border border-smoke bg-smoke px-4 py-2 text-sm text-cream outline-none placeholder:text-silver focus:border-esmeralda sm:w-56"
                />
                <button
                  onClick={join}
                  disabled={joining}
                  className="rounded-full bg-brand px-5 py-2 text-sm font-medium text-ink transition-opacity hover:opacity-90 disabled:opacity-60"
                >
                  {joining ? "Entrando…" : "Participar"}
                </button>
              </div>
            )}

            {actionError && <p className="mt-2 text-sm text-rosa">{actionError}</p>}
          </div>

          <div className="shrink-0">
            {!user ? (
              <Link
                to="/login"
                className="inline-block rounded-full bg-brand px-5 py-2.5 text-sm font-medium text-ink transition-opacity hover:opacity-90"
              >
                Entrar para participar
              </Link>
            ) : !user.verified ? (
              <Link
                to="/register"
                className="text-sm text-esmeralda underline hover:text-lima"
              >
                Verificar conta para participar
              </Link>
            ) : isMember ? (
              <button
                onClick={leave}
                disabled={leaving}
                className="rounded-full border border-smoke px-5 py-2.5 text-sm font-medium text-silver transition-colors hover:bg-smoke/60 hover:text-cream disabled:opacity-60"
              >
                {leaving ? "Saindo…" : "Sair"}
              </button>
            ) : !isPrivate ? (
              <button
                onClick={join}
                disabled={joining}
                className="rounded-full bg-brand px-5 py-2.5 text-sm font-medium text-ink transition-opacity hover:opacity-90 disabled:opacity-60"
              >
                {joining ? "Entrando…" : "Participar"}
              </button>
            ) : null}
          </div>
        </div>
      </section>

      {/* ══ GRADE: trilha de objetos + categorias │ painel │ membros + regras ══ */}
      <div className="mt-6 grid gap-6 lg:grid-cols-[220px_minmax(0,1fr)] xl:grid-cols-[220px_minmax(0,1fr)_280px]">
        {/* ── Trilha lateral (desktop): objetos da comunidade + categorias ── */}
        <aside className="hidden lg:block">
          <div className="sticky top-20 space-y-6">
            <nav className="space-y-1">
              {TABS.map((t) => {
                const active = tab === t.id;
                const count = counts[t.id] ?? 0;
                return (
                  <button
                    key={t.id}
                    type="button"
                    onClick={() => selectTab(t.id)}
                    className={`flex w-full items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition-colors ${
                      active
                        ? "bg-smoke text-cream"
                        : "text-silver hover:bg-smoke/60 hover:text-cream"
                    }`}
                  >
                    <span aria-hidden className="shrink-0">
                      {t.icon}
                    </span>
                    <span className="truncate">{t.label}</span>
                    {count > 0 && (
                      <span className="ml-auto shrink-0 text-xs text-silver">{count}</span>
                    )}
                  </button>
                );
              })}
            </nav>

            {communityCategories.length > 0 && (
              <section className="rounded-2xl border border-smoke bg-charcoal p-4">
                <h2 className="text-sm font-semibold text-cream">Categorias</h2>
                <ul className="mt-3 space-y-1">
                  <li>
                    <button
                      type="button"
                      aria-pressed={ofertaCat === null}
                      onClick={() => pickCategory(null)}
                      className={`grid w-full grid-cols-[minmax(0,1fr)_auto] items-center gap-2 rounded-lg px-2 py-1.5 text-left text-sm transition-colors ${
                        ofertaCat === null && tab === "ofertas"
                          ? "bg-smoke text-cream"
                          : "text-cream hover:bg-smoke/60"
                      }`}
                    >
                      <span className="truncate">Tudo</span>
                      <span className="shrink-0 text-xs text-silver">
                        {memberListings?.length ?? 0}
                      </span>
                    </button>
                  </li>
                  {communityCategories.map((c) => {
                    const active = ofertaCat === c.id && tab === "ofertas";
                    return (
                      <li key={c.id}>
                        <button
                          type="button"
                          aria-pressed={active}
                          onClick={() => pickCategory(c.id)}
                          className={`grid w-full grid-cols-[minmax(0,1fr)_auto] items-center gap-2 rounded-lg px-2 py-1.5 text-left text-sm transition-colors ${
                            active ? "bg-smoke text-cream" : "text-cream hover:bg-smoke/60"
                          }`}
                        >
                          <span className="truncate">{c.name}</span>
                          <span className="shrink-0 text-xs text-silver">{c.n}</span>
                        </button>
                      </li>
                    );
                  })}
                </ul>
              </section>
            )}
          </div>
        </aside>

        {/* ── Painel do objeto ativo ── */}
        <div className="min-w-0">
          {/* Abas de objetos no mobile (a trilha lateral cobre desktop) */}
          <nav className="sticky top-16 z-30 -mx-1 mb-4 border-b border-smoke bg-ink/85 px-1 backdrop-blur lg:hidden">
            <div className="flex gap-1 overflow-x-auto">
              {TABS.map((t) => {
                const active = tab === t.id;
                const count = counts[t.id] ?? 0;
                return (
                  <button
                    key={t.id}
                    type="button"
                    onClick={() => selectTab(t.id)}
                    className={`relative whitespace-nowrap px-4 py-3 text-sm font-medium transition ${
                      active ? "text-esmeralda" : "text-silver hover:text-cream"
                    }`}
                  >
                    <span aria-hidden className="mr-1.5">
                      {t.icon}
                    </span>
                    {t.label}
                    {count > 0 && (
                      <span
                        className={`ml-1.5 rounded-full px-1.5 py-0.5 text-xs ${
                          active ? "bg-esmeralda/15 text-esmeralda" : "bg-smoke text-silver"
                        }`}
                      >
                        {count}
                      </span>
                    )}
                    {active && (
                      <span className="absolute bottom-0 left-2 right-2 h-0.5 rounded-full bg-esmeralda" />
                    )}
                  </button>
                );
              })}
            </div>
          </nav>

          {tab === "conversas" && (
            <ConversasPanel
              isMember={isMember}
              canPost={!!user?.verified && isMember}
              nome={user?.nome ?? ""}
              posts={posts}
              rootsLoading={rootsLoading}
              newPost={newPost}
              setNewPost={setNewPost}
              posting={posting}
              submitRoot={submitRoot}
              onReply={handleReply}
              loadChildren={loadChildren}
              currentUserId={user?.userId}
              replyingId={replyingId ?? undefined}
            />
          )}

          {tab === "aovivo" && (
            <AoVivoPanel community={community} isMember={isMember} verified={!!user?.verified} />
          )}

          {tab === "ofertas" && (
            <OfertasPanel
              loading={memberListings === null}
              error={listingsError}
              items={visibleListings}
              total={memberListings?.length ?? 0}
              categories={communityCategories}
              selected={ofertaCat}
              onSelect={setOfertaCat}
              categoryById={categoryById}
              canPost={!!user?.verified}
            />
          )}

          {tab === "membros" && (
            <MembrosPanel members={activeMembers} currentUserId={user?.userId} />
          )}
        </div>

        {/* ── Coluna direita (desktop): membros em destaque + regras ── */}
        <aside className="hidden xl:block">
          <div className="sticky top-20 space-y-4">
            <section className="rounded-2xl border border-smoke bg-charcoal p-4">
              <h2 className="text-sm font-semibold text-cream">Membros</h2>
              <ul className="mt-3 space-y-3">
                {activeMembers.slice(0, 5).map((m) => (
                  <li key={m.Id} className="flex min-w-0 items-center gap-3">
                    <Avatar name={m.UserName} src={m.UserAvatarUrl} size={32} />
                    <div className="min-w-0">
                      <p className="truncate text-sm text-cream">{m.UserName}</p>
                      <p className="truncate text-xs text-silver">
                        {PAPEL_META[m.Role].label} · entrou {timeAgo(m.JoinedAt)}
                      </p>
                    </div>
                  </li>
                ))}
                {activeMembers.length === 0 && (
                  <li className="text-xs text-silver">Ninguém por aqui ainda.</li>
                )}
              </ul>
              {activeMembers.length > 5 && (
                <button
                  type="button"
                  onClick={() => selectTab("membros")}
                  className="mt-3 w-full rounded-full border border-smoke px-3 py-1.5 text-xs font-medium text-silver transition-colors hover:bg-smoke/60 hover:text-cream"
                >
                  Ver todos os {activeMembers.length} membros
                </button>
              )}
            </section>

            <section className="rounded-2xl border border-smoke bg-charcoal p-4">
              <h2 className="text-sm font-semibold text-cream">Regras da comunidade</h2>
              <ol className="mt-3 space-y-2">
                {[
                  "Respeite os membros: sem ofensas, discurso de ódio ou spam.",
                  "Venda ou troca? Publique como anúncio, com preço e categoria.",
                  "Conteúdo impróprio pode ser denunciado — a moderação analisa.",
                ].map((rule, i) => (
                  <li key={i} className="flex gap-2 text-xs leading-relaxed text-silver">
                    <span className="font-semibold text-esmeralda">{i + 1}.</span>
                    <span>{rule}</span>
                  </li>
                ))}
              </ol>
            </section>
          </div>
        </aside>
      </div>
    </div>
  );
}

/* ═══════════════════════════════════════════════════════════════════════
   Objeto: Conversas (Post — discussão assíncrona recursiva, UF-20)
   ═══════════════════════════════════════════════════════════════════════ */
function ConversasPanel({
  isMember,
  canPost,
  nome,
  posts,
  rootsLoading,
  newPost,
  setNewPost,
  posting,
  submitRoot,
  onReply,
  loadChildren,
  currentUserId,
  replyingId,
}: {
  isMember: boolean;
  canPost: boolean;
  nome: string;
  posts: Post[];
  rootsLoading: boolean;
  newPost: string;
  setNewPost: (v: string) => void;
  posting: boolean;
  submitRoot: (e: FormEvent) => void;
  onReply: (parentId: string, conteudo: string) => Promise<void>;
  loadChildren: (parentId: string) => Promise<Post[]>;
  currentUserId?: string;
  replyingId?: string;
}) {
  // Composer-pílula: colapsado vira um botão "No que você está pensando?".
  const [composing, setComposing] = useState(false);
  const primeiroNome = nome.split(" ")[0];

  return (
    <div className="space-y-4">
      {canPost && !composing && (
        <section className="rounded-2xl border border-smoke bg-charcoal p-4">
          <button
            type="button"
            onClick={() => setComposing(true)}
            className="w-full rounded-full bg-smoke px-4 py-2.5 text-left text-sm text-silver transition-colors hover:bg-smoke/70"
          >
            No que você está pensando, {primeiroNome}?
          </button>
        </section>
      )}

      {canPost && composing && (
        <form onSubmit={submitRoot} className="rounded-2xl border border-smoke bg-charcoal p-4">
          <textarea
            autoFocus
            value={newPost}
            onChange={(e) => setNewPost(e.target.value)}
            rows={3}
            placeholder="Compartilhe algo com a comunidade…"
            className="w-full rounded-lg border border-smoke bg-smoke px-3 py-2 text-sm text-cream outline-none focus:border-esmeralda"
          />
          <div className="mt-2 flex items-center gap-2">
            <button
              type="submit"
              disabled={posting || !newPost.trim()}
              className="rounded-full bg-brand px-4 py-2 text-sm font-semibold text-ink disabled:opacity-60"
            >
              {posting ? "Publicando…" : "Publicar"}
            </button>
            <button
              type="button"
              onClick={() => setComposing(false)}
              className="px-2 py-2 text-sm text-silver hover:text-cream"
            >
              Cancelar
            </button>
          </div>
        </form>
      )}

      {!isMember && (
        <div className="rounded-2xl border border-smoke bg-smoke/40 p-4 text-sm text-silver">
          💬 As conversas ficam visíveis para todos, mas só membros publicam e respondem.
        </div>
      )}

      {rootsLoading ? (
        <div className="space-y-4">
          {Array.from({ length: 3 }).map((_, i) => (
            <div key={i} className="h-36 animate-pulse rounded-2xl border border-smoke bg-smoke/50" />
          ))}
        </div>
      ) : posts.length === 0 ? (
        <EmptyState
          icon="💬"
          title={
            canPost
              ? "Ainda não há conversas. Que tal começar a falar com a comunidade?"
              : "Ainda não há conversas por aqui."
          }
        />
      ) : (
        <PostThread
          posts={posts}
          onReply={onReply}
          loadChildren={loadChildren}
          canPost={isMember}
          currentUserId={currentUserId}
          loadingId={replyingId}
        />
      )}
    </div>
  );
}

/* ═══════════════════════════════════════════════════════════════════════
   Objeto: Ao vivo (Chat — conversa em tempo real, UF-21)
   Atributo do objeto: mensagens expiram em 90 dias.
   ═══════════════════════════════════════════════════════════════════════ */
function AoVivoPanel({
  community,
  isMember,
  verified,
}: {
  community: Community;
  isMember: boolean;
  verified: boolean;
}) {
  return (
    <div>
      <div className="mb-3 flex flex-wrap items-center justify-between gap-3">
        <p className="text-sm text-silver">⚡ Conversa em tempo real com quem está online agora.</p>
        <span className="text-xs text-silver/70">As mensagens expiram em 90 dias</span>
      </div>
      <LiveChat communityId={community.Id} isMember={isMember} />
      {!isMember && (
        <p className="mt-3 text-center text-sm text-silver">
          Entre na comunidade para participar da conversa ao vivo. 🤝
        </p>
      )}
      {isMember && !verified && (
        <p className="mt-3 text-center text-sm text-silver">
          Confirme e-mail e telefone para conversar.
        </p>
      )}
    </div>
  );
}

/* ═══════════════════════════════════════════════════════════════════════
   Objeto: Anúncios (Listing — economia circular dos membros, UF-07..11)
   Publicações em coluna única, filtráveis por categoria (pílulas).
   ═══════════════════════════════════════════════════════════════════════ */
function OfertasPanel({
  loading,
  error,
  items,
  total,
  categories,
  selected,
  onSelect,
  categoryById,
  canPost,
}: {
  loading: boolean;
  error: boolean;
  items: FeedItem[];
  total: number;
  categories: { id: string; name: string; n: number }[];
  selected: string | null;
  onSelect: (cid: string | null) => void;
  categoryById: Map<string, string>;
  canPost: boolean;
}) {
  return (
    <div className="space-y-4">
      {categories.length > 0 && (
        <nav aria-label="Categorias" className="-mx-4 overflow-x-auto px-4 sm:mx-0 sm:px-0">
          <ul className="flex w-max gap-2 sm:w-auto sm:flex-wrap">
            <li>
              <button
                type="button"
                aria-pressed={selected === null}
                onClick={() => onSelect(null)}
                className={`whitespace-nowrap rounded-full border px-3.5 py-1.5 text-sm font-medium transition-colors ${
                  selected === null
                    ? "border-transparent bg-brand text-ink"
                    : "border-smoke text-silver hover:bg-smoke/60 hover:text-cream"
                }`}
              >
                Tudo
              </button>
            </li>
            {categories.map((c) => (
              <li key={c.id}>
                <button
                  type="button"
                  aria-pressed={selected === c.id}
                  onClick={() => onSelect(selected === c.id ? null : c.id)}
                  className={`whitespace-nowrap rounded-full border px-3.5 py-1.5 text-sm font-medium transition-colors ${
                    selected === c.id
                      ? "border-transparent bg-brand text-ink"
                      : "border-smoke text-silver hover:bg-smoke/60 hover:text-cream"
                  }`}
                >
                  {c.name}
                </button>
              </li>
            ))}
          </ul>
        </nav>
      )}

      {error ? (
        <div className="rounded-2xl border border-smoke bg-charcoal p-6 text-center text-sm text-silver">
          Não foi possível carregar os anúncios agora. Tente novamente mais tarde.
        </div>
      ) : loading ? (
        <div className="space-y-4">
          {Array.from({ length: 3 }).map((_, i) => (
            <div key={i} className="h-48 animate-pulse rounded-2xl border border-smoke bg-smoke/50" />
          ))}
        </div>
      ) : items.length === 0 ? (
        selected ? (
          <EmptyState
            icon="🛍️"
            title={`Nenhum anúncio em ${categoryById.get(selected) ?? "categoria"}.`}
          />
        ) : (
          <EmptyState
            icon="🛍️"
            title="Nenhum anúncio dos membros por aqui ainda."
            action={
              canPost ? (
                <Link
                  to="/listings/new"
                  className="inline-block rounded-full bg-brand px-5 py-2.5 text-sm font-semibold text-ink"
                >
                  + Anunciar algo
                </Link>
              ) : undefined
            }
          />
        )
      ) : (
        <div className="space-y-4">
          {items.map((item) => (
            <PublicationCard
              key={item.Id}
              item={item}
              categoryName={categoryById.get(item.CategoryId)}
            />
          ))}
        </div>
      )}

      {!loading && total > 0 && (
        <div className="text-center">
          <Link to="/feed" className="text-sm text-esmeralda hover:underline">
            Ver todos os anúncios no feed →
          </Link>
        </div>
      )}
    </div>
  );
}

/* ═══════════════════════════════════════════════════════════════════════
   Objeto: Membros (Membership — pessoas e papéis, UF-19)
   ═══════════════════════════════════════════════════════════════════════ */
function MembrosPanel({
  members,
  currentUserId,
}: {
  members: Membership[];
  currentUserId?: string;
}) {
  const [filtro, setFiltro] = useState<MembershipRole | "Todos">("Todos");

  const papeis: (MembershipRole | "Todos")[] = ["Todos", "Creator", "Moderator", "Member"];
  const visiveis =
    filtro === "Todos" ? members : members.filter((m) => m.Role === filtro);

  return (
    <div>
      <div className="mb-4 flex flex-wrap gap-2">
        {papeis.map((p) => {
          const n = p === "Todos" ? members.length : members.filter((m) => m.Role === p).length;
          const active = filtro === p;
          return (
            <button
              key={p}
              type="button"
              onClick={() => setFiltro(p)}
              className={`rounded-full border px-3 py-1.5 text-sm font-medium transition ${
                active
                  ? "border-transparent bg-esmeralda text-ink"
                  : "border-smoke text-silver hover:text-cream"
              }`}
            >
              {p === "Todos" ? "Todos" : PAPEL_META[p].label}
              {n > 0 && <span className="ml-1.5 opacity-70">{n}</span>}
            </button>
          );
        })}
      </div>

      {visiveis.length === 0 ? (
        <EmptyState
          icon="👥"
          title={
            filtro === "Todos"
              ? "Ninguém por aqui ainda."
              : `Nenhum ${PAPEL_META[filtro as MembershipRole].label.toLowerCase()} ainda.`
          }
        />
      ) : (
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          {visiveis.map((m) => {
            const papel = PAPEL_META[m.Role];
            const voce = currentUserId && m.UserId === currentUserId;
            return (
              <div
                key={m.Id}
                className="flex items-center gap-3 rounded-2xl border border-smoke bg-charcoal p-3"
              >
                <Avatar name={m.UserName} src={m.UserAvatarUrl} size={44} />
                <div className="min-w-0 flex-1">
                  <div className="flex items-center gap-1.5">
                    <span className="truncate text-sm font-semibold text-cream">
                      {m.UserName}
                    </span>
                    {voce && <span className="text-xs text-esmeralda">você</span>}
                  </div>
                  <div className="text-xs text-silver">entrou {timeAgo(m.JoinedAt)}</div>
                </div>
                <span
                  className={`rounded-full px-2 py-0.5 text-[10px] font-semibold ${papel.cls}`}
                >
                  {papel.label}
                </span>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
