import { useCallback, useEffect, useMemo, useState, type ChangeEvent } from "react";
import { Link, useLocation, useParams } from "react-router-dom";
import { ApiError, api } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import { Avatar } from "../components/Avatar";
import { EmptyState } from "../components/EmptyState";
import { ListingTimelineCard } from "../components/ListingTimelineCard";
import { LiveChat } from "../components/LiveChat";
import { PostComposer } from "../components/PostComposer";
import { PostThread } from "../components/PostThread";
import {
  EIXO_EMOJI,
  PAPEL_META,
  VISIBILIDADE_LABEL,
} from "../lib/community";
import type { Community, FeedItem, Membership, Post } from "../api/types";

/* ═══════════════════════════════════════════════════════════════════════
   Página única da comunidade (v2): filtros à esquerda, feed no centro com
   composer no topo (conversas e anúncios dos membros na MESMA timeline —
   anúncio é só um filtro), features à direita (chat ao vivo, membros,
   regras). Hashes antigos (#conversas #aovivo #ofertas #membros) viram
   âncoras — nada quebra.
   ═══════════════════════════════════════════════════════════════════════ */

// Hash legado "ofertas"/"anuncios" ancora na timeline central e liga o
// filtro de anúncios (a seção separada virou filtro do feed).
const ANCHOR_MAP: Record<string, string> = {
  conversas: "conversas",
  aovivo: "aovivo",
  ofertas: "conversas",
  anuncios: "conversas",
  membros: "membros",
};

const FILTRO_LABEL: Record<FiltroConversa, string> = {
  recentes: "🕘 Recentes",
  minhas: "✍️ Minhas",
  anuncios: "🛍️ Anúncios",
};

type FiltroConversa = "recentes" | "minhas" | "anuncios";

const REGRAS = [
  "Respeite os membros — sem ofensas, spam ou discurso de ódio.",
  "Anúncios dos membros aparecem no feed — use o filtro 🛍️ para vê-los.",
  "Denuncie conteúdo inadequado; moderadores cuidam do resto.",
];

export function CommunityDetailPage() {
  const { id } = useParams<{ id: string }>();
  const location = useLocation();
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
  const [replyingId, setReplyingId] = useState<string | null>(null);
  const [memberListings, setMemberListings] = useState<FeedItem[] | null>(null);
  const [filtro, setFiltro] = useState<FiltroConversa>("recentes");
  const [coverUploading, setCoverUploading] = useState(false);

  // Âncoras legadas (#hash) → scroll suave até a seção correspondente.
  // #ofertas/#anuncios também liga o filtro de anúncios da timeline.
  useEffect(() => {
    const raw = location.hash.replace(/^#/, "");
    const target = ANCHOR_MAP[raw];
    if (!target || loading) return;
    if (raw === "ofertas" || raw === "anuncios") setFiltro("anuncios");
    const t = requestAnimationFrame(() => {
      document.getElementById(target)?.scrollIntoView({ behavior: "smooth", block: "start" });
    });
    return () => cancelAnimationFrame(t);
  }, [location.hash, loading]);

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
  const myMembership = useMemo(
    () => (user ? activeMembers.find((m) => m.UserId === user.userId) : undefined),
    [activeMembers, user]
  );
  const isMember = !!myMembership;
  const canManageCover =
    myMembership?.Role === "Creator" || myMembership?.Role === "Moderator";
  const isPrivate = community?.Visibility === "Private";

  useEffect(() => {
    const ids = Array.from(new Set(activeMembers.map((m) => m.UserId).filter(Boolean)));
    if (ids.length === 0) {
      setMemberListings([]);
      return;
    }
    let active = true;
    setMemberListings(null);
    // Anúncios dos membros VISÍVEIS no contexto desta comunidade: communityId
    // faz o backend incluir também os escopados à comunidade (Visibility=
    // Community), que o feed sem esse filtro exclui. Na timeline mesclada a
    // falha degrada em silêncio (só conversas aparecem).
    api
      .feed({ sellerIds: ids.join(","), communityId: id, page: 1 })
      .then((items) => {
        if (active) setMemberListings(items);
      })
      .catch(() => {
        if (active) setMemberListings([]);
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

  // Composer no topo das conversas — o espaço de escrever vem antes da lista.
  async function submitRoot(conteudo: string) {
    if (!id || !user) return;
    setActionError(null);
    try {
      const createdId = await api.createPost(id, undefined, conteudo);
      const optimistic: Post = {
        Id: String(createdId),
        CommunityId: id,
        AutorId: user.userId,
        AuthorName: user.nome,
        AutorAvatarUrl: undefined,
        Content: conteudo,
        Path: String(createdId),
        Depth: 0,
        Status: "Visible",
        CreatedAt: new Date().toISOString(),
        LikeCount: 0,
        IsLiked: false,
        IsSaved: false,
      };
      setPosts((prev) => [optimistic, ...prev]);
      setFiltro("recentes");
    } catch (e) {
      setActionError(e instanceof ApiError ? e.message : "Falha ao publicar.");
      throw e; // o composer mantém o texto para retentar
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

  // Capa real: upload (folder communities) → setCommunityCover → refresh.
  async function onCoverFile(e: ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    e.target.value = ""; // permite reenviar o mesmo arquivo
    if (!file || !id) return;
    setCoverUploading(true);
    setActionError(null);
    try {
      const url = await api.uploadImage(file, "communities");
      await api.setCommunityCover(id, url);
      setCommunity(await api.community(id));
    } catch (err) {
      setActionError(err instanceof ApiError ? err.message : "Falha ao enviar a capa.");
    } finally {
      setCoverUploading(false);
    }
  }

  const visiblePosts = useMemo(
    () => (filtro === "minhas" ? posts.filter((p) => p.AutorId === user?.userId) : posts),
    [posts, filtro, user]
  );

  // Timeline mesclada: conversas (cada raiz com sua thread) e anúncios dos
  // membros intercalados por data. Filtro 🛍️ = só anúncios; ✍️ = só minhas.
  const timeline = useMemo(() => {
    const listingEntries = (memberListings ?? []).map((l) => ({
      tipo: "listing" as const,
      key: `l-${l.Id}`,
      at: l.CreatedAt ? new Date(l.CreatedAt).getTime() : 0,
      item: l,
    }));
    if (filtro === "anuncios") return listingEntries;
    const postEntries = visiblePosts.map((p) => ({
      tipo: "post" as const,
      key: `p-${p.Id}`,
      at: new Date(p.CreatedAt).getTime(),
      item: p,
    }));
    if (filtro === "minhas") return postEntries;
    return [...postEntries, ...listingEntries].sort((a, b) => b.at - a.at);
  }, [filtro, visiblePosts, memberListings]);

  const timelineLoading =
    rootsLoading || (filtro !== "minhas" && memberListings === null);

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
  const shareUrl = `${window.location.origin}/community/${community.Id}#conversas`;

  const composerHint = !user
    ? undefined
    : !user.verified
      ? "Verifique sua conta (e-mail e telefone) para publicar."
      : !isMember
        ? "Entre na comunidade para publicar e responder."
        : undefined;

  return (
    <div className="app-container">
      <Link
        to="/community"
        className="text-sm text-silver hover:text-cream mb-4 inline-block"
      >
        ← Comunidades
      </Link>

      {/* ══ HERO compacto — capa real (upload) com fallback gradiente ══ */}
      <header className="relative rounded-2xl overflow-hidden border border-smoke">
        {community.CoverImageUrl ? (
          <img
            src={community.CoverImageUrl}
            alt=""
            className="absolute inset-0 w-full h-full object-cover"
          />
        ) : (
          <div className="bg-community absolute inset-0" aria-hidden />
        )}
        <div className="absolute inset-0 bg-black/35" aria-hidden />
        <div className="relative p-4 sm:p-5 text-white">
          <div className="flex flex-wrap items-center gap-x-3 gap-y-2">
            <h1 className="flex items-center gap-2 text-xl sm:text-2xl font-bold drop-shadow">
              <span aria-hidden>{EIXO_EMOJI[community.Axis]}</span>
              <span className="truncate max-w-[16rem] sm:max-w-none">{community.Name}</span>
            </h1>
            {community.Type === "Default" && (
              <span className="text-[10px] font-semibold px-2 py-0.5 rounded-full bg-esmeralda/40 backdrop-blur-sm">
                Oficial
              </span>
            )}
            <span className="text-[10px] px-2 py-0.5 rounded-full bg-white/20 backdrop-blur-sm">
              {community.Visibility === "Private"
                ? `🔒 ${VISIBILIDADE_LABEL[community.Visibility]}`
                : VISIBILIDADE_LABEL[community.Visibility]}
            </span>

            <a
              href="#membros"
              className="text-sm text-white/90 hover:text-white ml-auto whitespace-nowrap"
            >
              👥 {community.MembersCount}
            </a>

            {!user ? (
              <Link
                to="/login"
                className="bg-white text-ink font-semibold px-4 py-1.5 rounded-lg text-sm hover:bg-white/90 whitespace-nowrap"
              >
                Entrar para participar
              </Link>
            ) : !user.verified ? (
              <Link
                to="/register"
                className="text-sm text-white/90 underline hover:text-white whitespace-nowrap"
              >
                Verificar conta para participar
              </Link>
            ) : isMember ? (
              <button
                onClick={leave}
                disabled={leaving}
                className="bg-white/15 hover:bg-white/25 backdrop-blur-sm border border-white/40 text-white font-semibold px-4 py-1.5 rounded-lg text-sm disabled:opacity-60 whitespace-nowrap"
              >
                {leaving ? "Saindo…" : "Sair"}
              </button>
            ) : !isPrivate ? (
              <button
                onClick={join}
                disabled={joining}
                className="bg-white text-ink font-semibold px-4 py-1.5 rounded-lg text-sm hover:bg-white/90 disabled:opacity-60 whitespace-nowrap"
              >
                {joining ? "Entrando…" : "Participar"}
              </button>
            ) : null}

            {local && (
              <span className="basis-full text-xs text-white/75 drop-shadow">📍 {local}</span>
            )}
          </div>

          {community.Description && (
            <p className="mt-2 max-w-3xl text-sm text-white/85 line-clamp-2 drop-shadow">
              {community.Description}
            </p>
          )}

          {/* Comunidade privada: senha + CTA em linha compacta abaixo do nome */}
          {user?.verified && !isMember && isPrivate && (
            <div className="mt-3 flex flex-col sm:flex-row sm:items-center gap-2">
              <input
                type="password"
                value={joinPassword}
                onChange={(e) => setJoinPassword(e.target.value)}
                placeholder="Senha da comunidade"
                className="bg-white/15 backdrop-blur-sm border border-white/40 rounded-lg px-3 py-1.5 outline-none text-sm text-white placeholder:text-white/70 sm:w-56"
              />
              <button
                onClick={join}
                disabled={joining}
                className="bg-white text-ink font-semibold px-4 py-1.5 rounded-lg text-sm hover:bg-white/90 disabled:opacity-60"
              >
                {joining ? "Entrando…" : "Participar"}
              </button>
            </div>
          )}

          {actionError && <p className="mt-2 text-sm text-rosa drop-shadow">{actionError}</p>}

          {canManageCover && (
            <label className="absolute bottom-3 right-3 cursor-pointer bg-black/45 backdrop-blur-sm border border-white/30 text-white text-xs font-semibold px-3 py-1.5 rounded-lg hover:bg-black/65 transition">
              {coverUploading ? "Enviando…" : "📷 Capa"}
              <input
                type="file"
                accept="image/jpeg,image/png,image/webp,image/gif"
                className="sr-only"
                onChange={onCoverFile}
                disabled={coverUploading}
              />
            </label>
          )}
        </div>
      </header>

      {/* ══ PÁGINA ÚNICA: filtros | conversas | features — full width ══ */}
      <div className="mt-4 grid gap-4 lg:grid-cols-[16rem_minmax(0,1fr)_20rem] items-start">
        {/* ── Esquerda: filtros do feed + atalhos ── */}
        <aside className="space-y-4">
          <section className="bg-charcoal rounded-xl border border-smoke p-4">
            <h2 className="text-xs font-bold uppercase tracking-wider text-silver mb-2">
              Feed da comunidade
            </h2>
            <div className="flex lg:flex-col gap-1.5 overflow-x-auto">
              {(Object.keys(FILTRO_LABEL) as FiltroConversa[]).map((f) => (
                <button
                  key={f}
                  type="button"
                  onClick={() => setFiltro(f)}
                  className={`whitespace-nowrap text-left px-3 py-1.5 rounded-lg text-sm transition ${
                    filtro === f
                      ? "bg-esmeralda/15 text-esmeralda font-medium"
                      : "text-silver hover:text-cream"
                  }`}
                >
                  {FILTRO_LABEL[f]}
                </button>
              ))}
            </div>
          </section>

          <section className="bg-charcoal rounded-xl border border-smoke p-4">
            <h2 className="text-xs font-bold uppercase tracking-wider text-silver mb-2">
              Atalhos
            </h2>
            <nav className="flex lg:flex-col gap-1.5 text-sm">
              <Link to="/saved" className="text-silver hover:text-cream px-3 py-1.5">
                🔖 Meus salvos
              </Link>
              {user?.verified && (
                <Link
                  to="/listings/new"
                  className="text-silver hover:text-cream px-3 py-1.5"
                >
                  ➕ Anunciar algo
                </Link>
              )}
            </nav>
          </section>
        </aside>

        {/* ── Centro: composer + conversas (a página principal) ── */}
        <main id="conversas" className="space-y-4 min-w-0">
          <PostComposer
            authorName={user?.nome ?? "Você"}
            onSubmit={submitRoot}
            disabled={!user?.verified || !isMember}
            disabledHint={composerHint ?? "Entre para participar das conversas."}
          />

          {!isMember && (
            <div className="bg-smoke/50 border border-smoke rounded-xl p-4 text-sm text-silver">
              💬 As conversas ficam visíveis para todos, mas só membros publicam e respondem.
            </div>
          )}

          {timelineLoading ? (
            <div className="space-y-2">
              {Array.from({ length: 3 }).map((_, i) => (
                <div key={i} className="h-24 bg-smoke rounded-lg animate-pulse" />
              ))}
            </div>
          ) : timeline.length === 0 ? (
            filtro === "anuncios" ? (
              <EmptyState
                icon="🛍️"
                title="Nenhum anúncio dos membros por aqui ainda."
                action={
                  user?.verified && isMember ? (
                    <Link
                      to="/listings/new"
                      className="inline-block bg-brand text-ink font-semibold px-5 py-2.5 rounded-xl"
                    >
                      + Anunciar algo
                    </Link>
                  ) : undefined
                }
              />
            ) : (
              <EmptyState
                icon="💬"
                title={
                  filtro === "minhas"
                    ? "Você ainda não publicou aqui."
                    : user?.verified && isMember
                      ? "Ainda não há conversas. Que tal começar a falar com a comunidade?"
                      : "Ainda não há conversas por aqui."
                }
              />
            )
          ) : (
            <div className="space-y-4">
              {timeline.map((e) =>
                e.tipo === "post" ? (
                  <PostThread
                    key={e.key}
                    posts={[e.item]}
                    onReply={handleReply}
                    loadChildren={loadChildren}
                    canPost={isMember}
                    currentUserId={user?.userId}
                    loadingId={replyingId ?? undefined}
                    viewerId={user?.userId}
                    shareUrl={shareUrl}
                  />
                ) : (
                  <ListingTimelineCard
                    key={e.key}
                    item={e.item}
                    currentUserId={user?.userId}
                  />
                )
              )}
            </div>
          )}
        </main>

        {/* ── Direita: chat ao vivo + membros + regras ── */}
        <aside className="space-y-4">
          <section id="aovivo" className="bg-charcoal rounded-xl border border-smoke p-4">
            <div className="flex items-baseline justify-between gap-2 mb-2">
              <h2 className="text-xs font-bold uppercase tracking-wider text-silver">
                ⚡ Ao vivo
              </h2>
              <span className="text-[10px] text-silver/70">expira em 90 dias</span>
            </div>
            <LiveChat communityId={community.Id} isMember={isMember} />
          </section>

          <section id="membros" className="bg-charcoal rounded-xl border border-smoke p-4">
            <h2 className="text-xs font-bold uppercase tracking-wider text-silver mb-2">
              👥 Membros ({activeMembers.length})
            </h2>
            <div className="max-h-96 overflow-y-auto pr-1 space-y-1.5">
              {activeMembers.map((m) => {
                const papel = PAPEL_META[m.Role];
                const voce = user && m.UserId === user.userId;
                return (
                  <Link
                    key={m.Id}
                    to={`/users/${m.UserId}`}
                    className="flex items-center gap-2.5 rounded-lg px-1 py-0.5 hover:bg-smoke/60 transition"
                  >
                    <Avatar name={m.UserName} src={m.UserAvatarUrl} size={28} />
                    <span className="text-sm text-cream truncate flex-1">
                      {m.UserName}
                      {voce && <span className="text-xs text-esmeralda ml-1">você</span>}
                    </span>
                    {m.Role !== "Member" && (
                      <span
                        className={`text-[10px] font-semibold px-2 py-0.5 rounded-full ${papel.cls}`}
                      >
                        {papel.label}
                      </span>
                    )}
                  </Link>
                );
              })}
            </div>
          </section>

          <section className="bg-charcoal rounded-xl border border-smoke p-4">
            <h2 className="text-xs font-bold uppercase tracking-wider text-silver mb-2">
              📜 Regras
            </h2>
            <ul className="space-y-1.5 text-sm text-silver">
              {REGRAS.map((r) => (
                <li key={r} className="flex gap-2">
                  <span aria-hidden>·</span>
                  <span>{r}</span>
                </li>
              ))}
            </ul>
          </section>
        </aside>
      </div>
    </div>
  );
}
