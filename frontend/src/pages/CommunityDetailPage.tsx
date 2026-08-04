import { useCallback, useEffect, useState, type FormEvent } from "react";
import { Link, useParams } from "react-router-dom";
import { ApiError, api } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import { Avatar } from "../components/Avatar";
import { LiveChat } from "../components/LiveChat";
import { PostThread } from "../components/PostThread";
import { timeAgo } from "../lib/time";
import { EIXO_EMOJI, EIXO_LABEL, PAPEL_META, VISIBILIDADE_LABEL } from "../lib/community";
import type { Community, Membership, Post } from "../api/types";

export function CommunityDetailPage() {
  const { id } = useParams<{ id: string }>();
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

  const activeMembers = members.filter((m) => m.Status === "Ativa");
  const isMember = !!(user && activeMembers.some((m) => m.UsuarioId === user.userId));
  const isPrivate = community?.Visibilidade === "Private";

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

  return (
    <div className="app-container">
      <Link
        to="/community"
        className="text-sm text-silver hover:text-cream mb-4 inline-block"
      >
        ← Comunidades
      </Link>

      <header className="bg-charcoal border border-smoke rounded-2xl p-5">
        <div className="flex items-center gap-1.5 flex-wrap mb-2">
          <span className="text-xs font-semibold px-2 py-0.5 rounded-full bg-amber/15 text-amber">
            {EIXO_EMOJI[community.Eixo]} {EIXO_LABEL[community.Eixo]}
          </span>
          {community.Tipo === "Default" && (
            <span className="text-xs px-2 py-0.5 rounded-full bg-esmeralda/15 text-esmeralda">
              Oficial
            </span>
          )}
          <span className="text-xs px-2 py-0.5 rounded-full bg-smoke text-silver">
            {community.Visibilidade === "Private"
              ? `🔒 ${VISIBILIDADE_LABEL[community.Visibilidade]}`
              : VISIBILIDADE_LABEL[community.Visibilidade]}
          </span>
        </div>
        <h1 className="text-2xl sm:text-3xl font-bold text-cream">{community.Nome}</h1>
        <p className="mt-2 text-cream/90 whitespace-pre-wrap">{community.Descricao}</p>

        <div className="mt-4 flex items-center gap-3 text-sm text-silver flex-wrap">
          <div className="flex items-center gap-2">
            <Avatar name={community.CriadorNome} src={community.CriadorAvatarUrl} size={24} />
            <span>
              por <span className="text-cream">{community.CriadorNome}</span>
            </span>
          </div>
          <span>
            · 👥 {community.MembrosCount}{" "}
            {community.MembrosCount === 1 ? "membro" : "membros"}
          </span>
          {local && <span>· 📍 {local}</span>}
        </div>

        <div className="mt-4">
          {!user ? (
            <Link
              to="/login"
              className="inline-block bg-brand text-ink font-semibold px-4 py-2 rounded-lg text-sm"
            >
              Entrar para participar
            </Link>
          ) : !user.verified ? (
            <p className="text-sm text-amber">
              Confirme e-mail e telefone para participar.{" "}
              <Link to="/register" className="underline">
                Verificar
              </Link>
            </p>
          ) : isMember ? (
            <button
              onClick={leave}
              disabled={leaving}
              className="border border-smoke text-cream font-semibold px-4 py-2 rounded-lg text-sm hover:border-rosa disabled:opacity-60"
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
                  className="bg-smoke text-cream rounded-lg border border-smoke focus:border-esmeralda px-3 py-2 outline-none text-sm sm:w-56"
                />
              )}
              <button
                onClick={join}
                disabled={joining}
                className="bg-brand text-ink font-semibold px-4 py-2 rounded-lg text-sm disabled:opacity-60"
              >
                {joining ? "Entrando…" : "Entrar"}
              </button>
            </div>
          )}
          {actionError && <p className="mt-2 text-sm text-rosa">{actionError}</p>}
        </div>
      </header>

      <div className="grid lg:grid-cols-3 gap-6 mt-6">
        <div className="lg:col-span-2 space-y-4">
          {isMember && (
            <form
              onSubmit={submitRoot}
              className="bg-charcoal border border-smoke rounded-xl p-4"
            >
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

          {rootsLoading ? (
            <div className="space-y-2">
              {Array.from({ length: 3 }).map((_, i) => (
                <div key={i} className="h-24 bg-smoke rounded-lg animate-pulse" />
              ))}
            </div>
          ) : posts.length === 0 ? (
            <div className="bg-charcoal border border-smoke rounded-xl p-8 text-center">
              <p className="text-silver">
                Ainda não há publicações.{" "}
                {isMember ? "Que tal começar a conversa?" : ""}
              </p>
            </div>
          ) : (
            <PostThread
              posts={posts}
              onReply={handleReply}
              loadChildren={loadChildren}
              canPost={isMember}
              currentUserId={user?.userId}
              loadingId={replyingId ?? undefined}
            />
          )}
        </div>

        <aside className="lg:col-span-1 space-y-4">
          <div className="bg-charcoal border border-smoke rounded-xl p-4">
            <h2 className="font-semibold text-cream mb-3">
              Membros ({activeMembers.length})
            </h2>
            {activeMembers.length === 0 ? (
              <p className="text-sm text-silver">Ninguém por aqui ainda.</p>
            ) : (
              <ul className="space-y-2 max-h-96 overflow-y-auto pr-1">
                {activeMembers.map((m) => {
                  const papel = PAPEL_META[m.Papel];
                  return (
                    <li key={m.Id} className="flex items-center gap-2.5">
                      <Avatar name={m.UsuarioNome} src={m.UsuarioAvatarUrl} size={32} />
                      <div className="min-w-0 flex-1">
                        <div className="text-sm text-cream truncate">{m.UsuarioNome}</div>
                        <div className="text-[10px] text-silver">
                          entrou {timeAgo(m.JoinedAt)}
                        </div>
                      </div>
                      <span
                        className={`text-[10px] font-semibold px-1.5 py-0.5 rounded-full ${papel.cls}`}
                      >
                        {papel.label}
                      </span>
                    </li>
                  );
                })}
              </ul>
            )}
          </div>
        </aside>
      </div>

      <section className="mt-8">
        <h2 className="text-lg font-bold text-cream mb-3">Conversa ao vivo</h2>
        <div className="max-w-2xl">
          <LiveChat comunidadeId={community.Id} isMember={isMember} />
        </div>
      </section>
    </div>
  );
}
