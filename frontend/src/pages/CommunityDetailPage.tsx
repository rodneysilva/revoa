import { useCallback, useEffect, useMemo, useState, type FormEvent } from "react";
import { Link, useLocation, useNavigate, useParams } from "react-router-dom";
import { ApiError, api } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import { Avatar } from "../components/Avatar";
import { ListingCard } from "../components/ListingCard";
import { LiveChat } from "../components/LiveChat";
import { PostThread } from "../components/PostThread";
import { timeAgo } from "../lib/time";
import {
  EIXO_EMOJI,
  EIXO_LABEL,
  PAPEL_META,
  VISIBILIDADE_LABEL,
} from "../lib/community";
import type { Community, FeedItem, Membership, PapelMembro, Post } from "../api/types";

/* ═══════════════════════════════════════════════════════════════════════
   Navegação por objetos (OOUX — objects-first).
   A Comunidade é o objeto central; cada aba é a "casa" de um objeto
   relacionado, com seus atributos e CTAs. Deep-link via #hash.
   Objetos: Conversas (Post) · Ao vivo (Chat) · Ofertas (Anúncio) · Membros (Membership).
   ═══════════════════════════════════════════════════════════════════════ */
type ObjectTab = "conversas" | "aovivo" | "ofertas" | "membros";

const TABS: { id: ObjectTab; icon: string; label: string }[] = [
  { id: "conversas", icon: "💬", label: "Conversas" },
  { id: "aovivo", icon: "⚡", label: "Ao vivo" },
  { id: "ofertas", icon: "🛍️", label: "Ofertas" },
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
    () => members.filter((m) => m.Status === "Ativa"),
    [members]
  );
  const isMember = !!(user && activeMembers.some((m) => m.UsuarioId === user.userId));
  const isPrivate = community?.Visibilidade === "Private";

  useEffect(() => {
    const ids = Array.from(new Set(activeMembers.map((m) => m.UsuarioId).filter(Boolean)));
    if (ids.length === 0) {
      setMemberListings([]);
      setListingsError(false);
      return;
    }
    let active = true;
    setMemberListings(null);
    setListingsError(false);
    api
      .feed({ vendedorIds: ids.join(","), page: 1 })
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
        ComunidadeId: id,
        AutorId: user.userId,
        AutorNome: user.nome,
        AutorAvatarUrl: undefined,
        Conteudo: c,
        Path: String(createdId),
        Depth: 0,
        Status: "Visivel",
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

  const local = [community.Bairro, community.Cidade, community.Estado]
    .filter(Boolean)
    .join(", ");

  const counts: Record<ObjectTab, number> = {
    conversas: posts.length,
    aovivo: 0,
    ofertas: memberListings?.length ?? 0,
    membros: activeMembers.length,
  };

  return (
    <div className="app-container">
      <Link
        to="/community"
        className="text-sm text-silver hover:text-cream mb-4 inline-block"
      >
        ← Comunidades
      </Link>

      {/* ══ HERO do objeto Comunidade ══ */}
      <header className="relative rounded-2xl overflow-hidden border border-smoke">
        <div className="bg-community absolute inset-0" aria-hidden />
        <div className="absolute inset-0 bg-black/30" aria-hidden />
        <div className="relative p-6 sm:p-8 text-white">
          <div className="flex items-center gap-1.5 flex-wrap mb-3">
            <span className="text-xs font-semibold px-2 py-0.5 rounded-full bg-white/25 backdrop-blur-sm">
              {EIXO_EMOJI[community.Eixo]} {EIXO_LABEL[community.Eixo]}
            </span>
            {community.Tipo === "Default" && (
              <span className="text-xs font-semibold px-2 py-0.5 rounded-full bg-esmeralda/40 backdrop-blur-sm">
                Oficial
              </span>
            )}
            <span className="text-xs px-2 py-0.5 rounded-full bg-white/20 backdrop-blur-sm">
              {community.Visibilidade === "Private"
                ? `🔒 ${VISIBILIDADE_LABEL[community.Visibilidade]}`
                : VISIBILIDADE_LABEL[community.Visibilidade]}
            </span>
          </div>

          <h1 className="text-2xl sm:text-4xl font-bold drop-shadow">{community.Nome}</h1>
          {community.Descricao && (
            <p className="mt-2 max-w-3xl text-white/90 whitespace-pre-wrap drop-shadow">
              {community.Descricao}
            </p>
          )}

          <div className="mt-4 flex items-center gap-x-4 gap-y-2 text-sm flex-wrap">
            <div className="flex items-center gap-2">
              <Avatar name={community.CriadorNome} src={community.CriadorAvatarUrl} size={26} />
              <span className="text-white/90">
                por <span className="font-semibold">{community.CriadorNome}</span>
              </span>
            </div>
            <button
              type="button"
              onClick={() => selectTab("membros")}
              className="text-white/90 hover:text-white"
            >
              👥 {community.MembrosCount}{" "}
              {community.MembrosCount === 1 ? "membro" : "membros"}
            </button>
            {local && <span className="text-white/90">📍 {local}</span>}
          </div>

          <div className="mt-5">
            {!user ? (
              <Link
                to="/login"
                className="inline-block bg-white text-ink font-semibold px-5 py-2.5 rounded-xl text-sm hover:bg-white/90"
              >
                Entrar para participar
              </Link>
            ) : !user.verified ? (
              <p className="text-sm bg-white/15 inline-block px-3 py-2 rounded-lg">
                Confirme e-mail e telefone para participar.{" "}
                <Link to="/register" className="underline font-semibold">
                  Verificar
                </Link>
              </p>
            ) : isMember ? (
              <button
                onClick={leave}
                disabled={leaving}
                className="bg-white/15 hover:bg-white/25 backdrop-blur-sm border border-white/40 text-white font-semibold px-5 py-2.5 rounded-xl text-sm disabled:opacity-60"
              >
                {leaving ? "Saindo…" : "Sair da comunidade"}
              </button>
            ) : (
              <div className="flex flex-col sm:flex-row sm:items-center gap-2">
                {isPrivate && (
                  <input
                    type="password"
                    value={joinPassword}
                    onChange={(e) => setJoinPassword(e.target.value)}
                    placeholder="Senha da comunidade"
                    className="bg-white/15 backdrop-blur-sm border border-white/40 rounded-xl px-3 py-2.5 outline-none text-sm text-white placeholder:text-white/70 sm:w-56"
                  />
                )}
                <button
                  onClick={join}
                  disabled={joining}
                  className="bg-white text-ink font-semibold px-5 py-2.5 rounded-xl text-sm hover:bg-white/90 disabled:opacity-60"
                >
                  {joining ? "Entrando…" : "Entrar na comunidade"}
                </button>
              </div>
            )}
            {actionError && <p className="mt-2 text-sm text-rosa drop-shadow">{actionError}</p>}
          </div>
        </div>
      </header>

      {/* ══ NAVEGAÇÃO POR OBJETOS (sticky) ══ */}
      <nav className="sticky top-16 z-30 mt-5 -mx-1 px-1 bg-ink/85 backdrop-blur border-b border-smoke">
        <div className="flex gap-1 overflow-x-auto">
          {TABS.map((t) => {
            const active = tab === t.id;
            const count = counts[t.id];
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
                {t.id !== "aovivo" && count > 0 && (
                  <span
                    className={`ml-1.5 text-xs px-1.5 py-0.5 rounded-full ${
                      active ? "bg-esmeralda/15 text-esmeralda" : "bg-smoke text-silver"
                    }`}
                  >
                    {count}
                  </span>
                )}
                {t.id === "aovivo" && isMember && (
                  <span
                    className="ml-1.5 inline-block w-1.5 h-1.5 rounded-full bg-esmeralda animate-pulse align-middle"
                    title="Ao vivo"
                  />
                )}
                {active && (
                  <span className="absolute left-2 right-2 -bottom-px h-0.5 bg-esmeralda rounded-full" />
                )}
              </button>
            );
          })}
        </div>
      </nav>

      {/* ══ PAINEL DO OBJETO ATIVO ══ */}
      <div className="mt-6">
        {tab === "conversas" && (
          <ConversasPanel
            isMember={isMember}
            canPost={!!user?.verified && isMember}
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
            items={memberListings ?? []}
            isMember={isMember}
            canPost={!!user?.verified}
          />
        )}

        {tab === "membros" && (
          <MembrosPanel members={activeMembers} currentUserId={user?.userId} />
        )}
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
  return (
    <div className="max-w-3xl space-y-4">
      {canPost && (
        <form onSubmit={submitRoot} className="bg-charcoal border border-smoke rounded-xl p-4">
          <textarea
            value={newPost}
            onChange={(e) => setNewPost(e.target.value)}
            rows={3}
            placeholder="Compartilhe algo com a comunidade…"
            className="w-full bg-smoke text-cream rounded-lg border border-smoke focus:border-esmeralda px-3 py-2 outline-none text-sm"
          />
          <div className="mt-2 flex items-center gap-2">
            <button
              type="submit"
              disabled={posting || !newPost.trim()}
              className="bg-brand text-ink font-semibold px-4 py-1.5 rounded-lg text-sm disabled:opacity-60"
            >
              {posting ? "Publicando…" : "Publicar"}
            </button>
          </div>
        </form>
      )}

      {!isMember && (
        <div className="bg-smoke/50 border border-smoke rounded-xl p-4 text-sm text-silver">
          💬 As conversas ficam visíveis para todos, mas só membros publicam e respondem.
        </div>
      )}

      {rootsLoading ? (
        <div className="space-y-2">
          {Array.from({ length: 3 }).map((_, i) => (
            <div key={i} className="h-24 bg-smoke rounded-lg animate-pulse" />
          ))}
        </div>
      ) : posts.length === 0 ? (
        <div className="bg-charcoal border border-smoke rounded-xl p-8 text-center">
          <div className="text-3xl mb-2" aria-hidden>
            💬
          </div>
          <p className="text-silver">
            {canPost ? "Ainda não há conversas. Que tal começar a falar com a comunidade?" : "Ainda não há conversas por aqui."}
          </p>
        </div>
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
    <div className="max-w-3xl">
      <div className="flex items-center justify-between gap-3 mb-3 flex-wrap">
        <p className="text-sm text-silver">
          ⚡ Conversa em tempo real com quem está online agora.
        </p>
        <span className="text-xs text-silver/70">As mensagens expiram em 90 dias</span>
      </div>
      <LiveChat comunidadeId={community.Id} isMember={isMember} />
      {!isMember && (
        <p className="mt-3 text-sm text-silver text-center">
          Entre na comunidade para participar da conversa ao vivo. 🤝
        </p>
      )}
      {isMember && !verified && (
        <p className="mt-3 text-sm text-silver text-center">
          Confirme e-mail e telefone para conversar.
        </p>
      )}
    </div>
  );
}

/* ═══════════════════════════════════════════════════════════════════════
   Objeto: Ofertas (Anúncio — economia circular dos membros, UF-07..11)
   ═══════════════════════════════════════════════════════════════════════ */
function OfertasPanel({
  loading,
  error,
  items,
  isMember,
  canPost,
}: {
  loading: boolean;
  error: boolean;
  items: FeedItem[];
  isMember: boolean;
  canPost: boolean;
}) {
  return (
    <div>
      {error ? (
        <div className="bg-charcoal border border-smoke rounded-xl p-6 text-center text-sm text-silver">
          Não foi possível carregar as ofertas agora. Tente novamente mais tarde.
        </div>
      ) : loading ? (
        <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4">
          {Array.from({ length: 8 }).map((_, i) => (
            <div key={i} className="aspect-[3/4] bg-smoke rounded-xl animate-pulse" />
          ))}
        </div>
      ) : items.length === 0 ? (
        <div className="bg-charcoal border border-smoke rounded-2xl p-8 text-center">
          <div className="text-3xl mb-2" aria-hidden>
            🛍️
          </div>
          <p className="text-silver">
            Nenhum anúncio dos membros por aqui ainda.
          </p>
          {canPost && (
            <Link
              to="/listings/new"
              className="mt-4 inline-block bg-brand text-ink font-semibold px-5 py-2.5 rounded-xl"
            >
              + Anunciar algo
            </Link>
          )}
        </div>
      ) : (
        <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4">
          {items.map((item) => (
            <ListingCard key={item.Id} item={item} />
          ))}
        </div>
      )}

      {items.length > 0 && (
        <div className="mt-5 text-center">
          <Link to="/feed" className="text-sm text-esmeralda hover:underline">
            Ver todos os anúncios no feed →
          </Link>
        </div>
      )}
      {isMember && items.length > 0 && canPost && (
        <p className="mt-2 text-xs text-silver text-center">
          Quer compartilhar algo?{" "}
          <Link to="/listings/new" className="text-esmeralda hover:underline">
            Criar anúncio
          </Link>
        </p>
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
  const [filtro, setFiltro] = useState<PapelMembro | "Todos">("Todos");

  const papeis: (PapelMembro | "Todos")[] = ["Todos", "Criador", "Moderador", "Membro"];
  const visiveis =
    filtro === "Todos" ? members : members.filter((m) => m.Papel === filtro);

  return (
    <div>
      <div className="flex gap-2 flex-wrap mb-4">
        {papeis.map((p) => {
          const n = p === "Todos" ? members.length : members.filter((m) => m.Papel === p).length;
          const active = filtro === p;
          return (
            <button
              key={p}
              type="button"
              onClick={() => setFiltro(p)}
              className={`px-3 py-1.5 rounded-full text-sm font-medium border transition ${
                active
                  ? "bg-esmeralda text-ink border-transparent"
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
        <div className="bg-charcoal border border-smoke rounded-xl p-8 text-center">
          <div className="text-3xl mb-2" aria-hidden>
            👥
          </div>
          <p className="text-silver">
            {filtro === "Todos"
              ? "Ninguém por aqui ainda."
              : `Nenhum ${PAPEL_META[filtro as PapelMembro].label.toLowerCase()} ainda.`}
          </p>
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
          {visiveis.map((m) => {
            const papel = PAPEL_META[m.Papel];
            const voce = currentUserId && m.UsuarioId === currentUserId;
            return (
              <div
                key={m.Id}
                className="flex items-center gap-3 bg-charcoal border border-smoke rounded-xl p-3"
              >
                <Avatar name={m.UsuarioNome} src={m.UsuarioAvatarUrl} size={44} />
                <div className="min-w-0 flex-1">
                  <div className="flex items-center gap-1.5">
                    <span className="text-sm font-semibold text-cream truncate">
                      {m.UsuarioNome}
                    </span>
                    {voce && <span className="text-xs text-esmeralda">você</span>}
                  </div>
                  <div className="text-xs text-silver">entrou {timeAgo(m.JoinedAt)}</div>
                </div>
                <span
                  className={`text-[10px] font-semibold px-2 py-0.5 rounded-full ${papel.cls}`}
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
