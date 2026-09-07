import { useCallback, useEffect, useMemo, useState, type ChangeEvent } from "react";
import { Link, useLocation, useParams } from "react-router-dom";
import {
  Bookmark,
  Camera,
  Clock,
  Eye,
  Lock,
  MapPin,
  MessageCircle,
  Plus,
  ScrollText,
  ShoppingBag,
  SquarePen,
  Users,
} from "lucide-react";
import { ApiError, api } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import { Avatar } from "../components/Avatar";
import { EmptyState } from "../components/EmptyState";
import { ListingTimelineCard } from "../components/ListingTimelineCard";
import { LiveChat } from "../components/LiveChat";
import { PostCard } from "../components/PostCard";
import { PostComposer } from "../components/PostComposer";
import { PostThread } from "../components/PostThread";
import {
  EIXO_ICON,
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
  recentes: "Recentes",
  minhas: "Minhas",
  anuncios: "Anúncios",
  salvos: "Salvos",
};
const FILTRO_ICON: Record<FiltroConversa, typeof Clock> = {
  recentes: Clock,
  minhas: SquarePen,
  anuncios: ShoppingBag,
  salvos: Bookmark,
};

type FiltroConversa = "recentes" | "minhas" | "anuncios" | "salvos";

const REGRAS = [
  "Respeite os membros — sem ofensas, spam ou discurso de ódio.",
  "Anúncios dos membros aparecem no feed — use o filtro Anúncios para vê-los.",
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
  // Itens salvos pelo usuário (carregados só quando o filtro 🔖 é acionado).
  const [savedPostsList, setSavedPostsList] = useState<Post[] | null>(null);
  const [savedListingsList, setSavedListingsList] = useState<FeedItem[] | null>(null);
  // Bootstrap dos estados dos cards: curtidos/salvos de anúncio (uma chamada).
  const [likedListingIds, setLikedListingIds] = useState<Set<string>>(new Set());
  const [savedListingIds, setSavedListingIds] = useState<Set<string>>(new Set());
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
    let active = true;
    setMemberListings(null);
    // Anúncios DA comunidade: onlyCommunity traz só os escopados a ela — os
    // públicos dos membros ficam no feed da rede, não aqui. A falha degrada
    // em silêncio (só conversas aparecem).
    api
      .feed({ communityId: id, onlyCommunity: true, page: 1 })
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

  // Bootstrap dos marcadores de anúncio (curtado/salvo) — uma chamada por
  // tipo, sem N+1 por card. Anônimo não carrega nada.
  useEffect(() => {
    if (!user) {
      setLikedListingIds(new Set());
      setSavedListingIds(new Set());
      return;
    }
    let active = true;
    api.likedListingIds().then((ids) => {
      if (active) setLikedListingIds(new Set(ids));
    }).catch(() => {});
    api.savedListingIds().then((ids) => {
      if (active) setSavedListingIds(new Set(ids));
    }).catch(() => {});
    return () => {
      active = false;
    };
  }, [user]);

  // Filtro 🔖 Salvos: itens salvos DESTA comunidade (posts salvos aqui +
  // anúncios salvos que estão entre os anúncios da comunidade). Buscado a
  // CADA ativação do filtro — salvar/dessalvar em outro filtro reflete na
  // hora, sem refresh.
  useEffect(() => {
    if (filtro !== "salvos" || !user) return;
    let active = true;
    api
      .savedPosts()
      .then((all) => {
        if (active)
          setSavedPostsList(all.filter((p) => p.CommunityId === id));
      })
      .catch(() => {
        if (active) setSavedPostsList([]);
      });
    api
      .savedListings()
      .then((all) => {
        if (active) {
          const daComunidade = new Set((memberListings ?? []).map((l) => l.Id));
          setSavedListingsList(all.filter((l) => daComunidade.has(l.Id)));
        }
      })
      .catch(() => {
        if (active) setSavedListingsList([]);
      });
    return () => {
      active = false;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [filtro, user, id, memberListings]);

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

  // Timeline mesclada da comunidade: conversas e anúncios DELA intercalados
  // por data. 🛍️ = só anúncios; ✍️ = só minhas; 🔖 = só o que salvei daqui.
  const timeline = useMemo(() => {
    const listingEntries = (memberListings ?? []).map((l) => ({
      tipo: "listing" as const,
      key: `l-${l.Id}`,
      at: l.CreatedAt ? new Date(l.CreatedAt).getTime() : 0,
      item: l,
      salvo: false as const,
    }));
    if (filtro === "anuncios") return listingEntries;
    if (filtro === "salvos") {
      return [
        ...(savedPostsList ?? []).map((p) => ({
          tipo: "post" as const,
          key: `sp-${p.Id}`,
          at: new Date(p.CreatedAt).getTime(),
          item: p,
          salvo: true as const,
        })),
        ...(savedListingsList ?? []).map((l) => ({
          tipo: "listing" as const,
          key: `sl-${l.Id}`,
          at: l.CreatedAt ? new Date(l.CreatedAt).getTime() : 0,
          item: l,
          salvo: true as const,
        })),
      ].sort((a, b) => b.at - a.at);
    }
    const postEntries = visiblePosts.map((p) => ({
      tipo: "post" as const,
      key: `p-${p.Id}`,
      at: new Date(p.CreatedAt).getTime(),
      item: p,
      salvo: false as const,
    }));
    if (filtro === "minhas") return postEntries;
    return [...postEntries, ...listingEntries].sort((a, b) => b.at - a.at);
  }, [filtro, visiblePosts, memberListings, savedPostsList, savedListingsList]);

  const timelineLoading =
    rootsLoading ||
    (filtro !== "minhas" && filtro !== "salvos" && memberListings === null) ||
    (filtro === "salvos" && (savedPostsList === null || savedListingsList === null));

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

  const AxisIcon = EIXO_ICON[community.Axis];
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

  // Por que não dá para interagir (curtir/comentar/salvar): comunidade
  // pública mostra tudo, mas a interação é de membro verificado.
  const interactHint = !user
    ? "Entre para interagir."
    : !user.verified
      ? "Verifique sua conta (e-mail e telefone) para interagir."
      : !isMember
        ? "Entre na comunidade para curtir, comentar e salvar."
        : undefined;

  return (
    <div className="app-container">
      <Link
        to="/community"
        className="text-sm text-silver hover:text-cream mb-4 inline-block"
      >
        ← Comunidades
      </Link>

      {/* ══ HERO — capa real (upload) com fallback gradiente ══
          Proporcional à capa (seeds 1200×400 = 3:1): cresce com a largura do
          container (2880px → 960px de altura em 4K) e nunca corta a imagem.
          Piso de 13rem só para telas estreitas, onde 3:1 ficaria baixo demais
          para a faixa de título. */}
      <header className="relative rounded-2xl overflow-hidden border border-smoke aspect-[3/1] min-h-[13rem] flex flex-col">
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
        <div className="relative p-4 sm:p-5 mt-auto text-white">
          <div className="flex flex-wrap items-center gap-x-3 gap-y-2">
            <h1 className="flex items-center gap-2 text-xl sm:text-2xl font-bold drop-shadow">
              <AxisIcon aria-hidden className="w-5 h-5" />
              <span className="truncate max-w-[16rem] sm:max-w-none">{community.Name}</span>
            </h1>
            {community.Type === "Default" && (
              <span className="text-[10px] font-semibold px-2 py-0.5 rounded-full bg-esmeralda/40 backdrop-blur-sm">
                Oficial
              </span>
            )}
            <span className="inline-flex items-center gap-1 text-[10px] px-2 py-0.5 rounded-full bg-white/20 backdrop-blur-sm">
              {community.Visibility === "Private" && <Lock aria-hidden className="w-3 h-3" />}
              {VISIBILIDADE_LABEL[community.Visibility]}
            </span>

            <a
              href="#membros"
              className="inline-flex items-center gap-1 text-sm text-white/90 hover:text-white ml-auto whitespace-nowrap"
            >
              <Users aria-hidden className="w-4 h-4" />
              {community.MembersCount}
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
              <span className="basis-full inline-flex items-center gap-1 text-xs text-white/75 drop-shadow">
                <MapPin aria-hidden className="w-3 h-3" />
                {local}
              </span>
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
              {coverUploading ? (
                "Enviando…"
              ) : (
                <>
                  <Camera aria-hidden className="w-3.5 h-3.5" />
                  Capa
                </>
              )}
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

      {/* ══ PÁGINA ÚNICA: filtros | feed | features — full width ══ */}
      <div className="mt-4 grid gap-4 lg:grid-cols-[16rem_minmax(0,1fr)_22rem] items-start">
        {/* ── Esquerda: filtros do feed + atalhos (acompanha o scroll) ── */}
        <aside className="space-y-4 lg:sticky lg:top-20 self-start">
          <section className="bg-charcoal rounded-xl border border-smoke p-4">
            <h2 className="text-xs font-bold uppercase tracking-wider text-silver mb-2">
              Feed da comunidade
            </h2>
            <div className="flex lg:flex-col gap-1.5 overflow-x-auto">
              {(Object.keys(FILTRO_LABEL) as FiltroConversa[]).map((f) => {
                const FIcon = FILTRO_ICON[f];
                return (
                  <button
                    key={f}
                    type="button"
                    onClick={() => setFiltro(f)}
                    className={`inline-flex items-center gap-1.5 whitespace-nowrap text-left px-3 py-1.5 rounded-lg text-sm transition ${
                      filtro === f
                        ? "bg-esmeralda/15 text-esmeralda font-medium"
                        : "text-silver hover:text-cream"
                    }`}
                  >
                    <FIcon aria-hidden className="w-3.5 h-3.5 shrink-0" />
                    {FILTRO_LABEL[f]}
                  </button>
                );
              })}
            </div>
          </section>

          <section className="bg-charcoal rounded-xl border border-smoke p-4">
            <h2 className="text-xs font-bold uppercase tracking-wider text-silver mb-2">
              Atalhos
            </h2>
            <nav className="flex lg:flex-col gap-1.5 text-sm">
              {user?.verified && (
                <Link
                  to={`/listings/new?community=${community.Id}`}
                  className="inline-flex items-center gap-1.5 text-silver hover:text-cream px-3 py-1.5"
                >
                  <Plus aria-hidden className="w-3.5 h-3.5" /> Anunciar algo
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
              <span className="inline-flex items-start gap-2">
                <Eye aria-hidden className="w-4 h-4 mt-0.5 shrink-0" />
                Comunidade pública: conversas e anúncios aparecem para todos —
                só membros publicam, comentam, curtem e salvam.
              </span>
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
                icon={<ShoppingBag className="w-8 h-8" />}
                title="Nenhum anúncio da comunidade por aqui ainda."
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
            ) : filtro === "salvos" ? (
              <EmptyState
                icon={<Bookmark className="w-8 h-8" />}
                title={
                  user
                    ? "Você ainda não salvou nada desta comunidade."
                    : "Entre para salvar conversas e anúncios."
                }
              />
            ) : (
              <EmptyState
                icon={<MessageCircle className="w-8 h-8" />}
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
                  e.salvo ? (
                    <PostCard
                      key={e.key}
                      post={e.item}
                      currentUserId={user?.userId}
                      onUnsave={() =>
                        setSavedPostsList((prev) =>
                          (prev ?? []).filter((p) => p.Id !== e.item.Id)
                        )
                      }
                    />
                  ) : (
                    <PostThread
                      key={e.key}
                      posts={[e.item]}
                      onReply={handleReply}
                      loadChildren={loadChildren}
                      canPost={isMember}
                      currentUserId={user?.userId}
                      loadingId={replyingId ?? undefined}
                      viewerId={isMember ? user?.userId : undefined}
                      shareUrl={shareUrl}
                    />
                  )
                ) : (
                  <ListingTimelineCard
                    key={e.key}
                    item={e.item}
                    currentUserId={user?.userId}
                    interactive
                    canInteract={!!user?.verified && isMember}
                    interactHint={interactHint}
                    initialLiked={likedListingIds.has(e.item.Id)}
                    initialSaved={e.salvo || savedListingIds.has(e.item.Id)}
                    onUnsave={
                      e.salvo
                        ? () =>
                            setSavedListingsList((prev) =>
                              (prev ?? []).filter((l) => l.Id !== e.item.Id)
                            )
                        : undefined
                    }
                  />
                )
              )}
            </div>
          )}
        </main>

        {/* ── Direita: chat + membros + regras (acompanha o scroll) ── */}
        <aside className="space-y-4 lg:sticky lg:top-20 self-start">
          {/* Chat — um box só (o LiveChat já desenha a moldura e a legenda) */}
          <div id="aovivo" className="scroll-mt-24">
            <LiveChat communityId={community.Id} isMember={isMember} />
          </div>

          <section id="membros" className="bg-charcoal rounded-xl border border-smoke p-4">
            <h2 className="text-xs font-bold uppercase tracking-wider text-silver mb-2">
              <span className="inline-flex items-center gap-1.5">
                <Users aria-hidden className="w-3.5 h-3.5" />
                Membros ({activeMembers.length})
              </span>
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
              <span className="inline-flex items-center gap-1.5">
                <ScrollText aria-hidden className="w-3.5 h-3.5" />
                Regras
              </span>
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
