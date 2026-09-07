import { useState } from "react";
import { Link } from "react-router-dom";
import { Avatar } from "./Avatar";
import { ReportButton } from "./ReportButton";
import { ShareButton } from "./ShareButton";
import { timeAgo } from "../lib/time";
import { api } from "../api/client";
import type { Post, ReportTarget } from "../api/types";

const MAX_DEPTH = 6;

interface ThreadHandlers {
  onReply: (parentId: string, conteudo: string) => Promise<void>;
  loadChildren: (parentId: string) => Promise<Post[]>;
  canPost: boolean;
  currentUserId?: string;
  loadingId?: string;
  // O que esta thread denuncia: "Post" (comunidade) ou "Comment" (anúncio).
  reportTarget?: ReportTarget;
  // Action bar (comunidade/feed): viewerId logado habilita curtir/salvar
  // otimistas. Ausente = thread só leitura (ex.: comentários de anúncio).
  viewerId?: string;
  // URL alvo do compartilhar; ausente = página atual.
  shareUrl?: string;
}

interface PostThreadProps extends ThreadHandlers {
  posts: Post[];
}

interface ItemProps extends ThreadHandlers {
  post: Post;
}

export function PostThread({ posts, ...handlers }: PostThreadProps) {
  if (posts.length === 0) return null;
  return (
    <div className="space-y-2">
      {posts.map((p) => (
        <PostItem key={p.Id} post={p} {...handlers} />
      ))}
    </div>
  );
}

function PostItem({
  post,
  onReply,
  loadChildren,
  canPost,
  currentUserId,
  loadingId,
  reportTarget,
  viewerId,
  shareUrl,
}: ItemProps) {
  const [showReply, setShowReply] = useState(false);
  const [text, setText] = useState("");
  const [open, setOpen] = useState(false);
  const [children, setChildren] = useState<Post[] | null>(null);
  const [loadingKids, setLoadingKids] = useState(false);

  // Action bar: estado otimista com rollback (mesmo padrão do PostCard).
  const [liked, setLiked] = useState(post.IsLiked ?? false);
  const [likeCount, setLikeCount] = useState(post.LikeCount ?? 0);
  const [saved, setSaved] = useState(post.IsSaved ?? false);
  const [busy, setBusy] = useState<"like" | "save" | null>(null);

  const canReply = canPost && post.Depth < MAX_DEPTH;
  const submitting = loadingId === post.Id;
  const childCount = post.ChildrenCount ?? 0;
  const knownZero = post.ChildrenCount === 0;
  const showRepliesToggle =
    !knownZero || open || (children != null && children.length > 0);
  const repliesLabel =
    childCount > 0
      ? `💬 ${childCount} resposta${childCount === 1 ? "" : "s"}`
      : "Ver respostas";

  async function toggleLike() {
    if (!viewerId || busy) return;
    setBusy("like");
    const next = !liked;
    setLiked(next);
    setLikeCount((c) => c + (next ? 1 : -1));
    try {
      const on = await api.likePost(post.Id);
      if (on !== next) {
        setLiked(on);
        setLikeCount((c) => c + (on ? 1 : -1));
      }
    } catch {
      setLiked(!next);
      setLikeCount((c) => c + (next ? -1 : 1));
    } finally {
      setBusy(null);
    }
  }

  async function toggleSave() {
    if (!viewerId || busy) return;
    setBusy("save");
    const next = !saved;
    setSaved(next);
    try {
      const on = await api.savePost(post.Id);
      if (on !== next) setSaved(on);
    } catch {
      setSaved(!next);
    } finally {
      setBusy(null);
    }
  }

  async function toggleChildren() {
    if (open) {
      setOpen(false);
      return;
    }
    if (children === null) {
      setLoadingKids(true);
      try {
        setChildren(await loadChildren(post.Id));
      } catch {
        setChildren([]);
      } finally {
        setLoadingKids(false);
      }
    }
    setOpen(true);
  }

  async function submit() {
    const c = text.trim();
    if (!c) return;
    await onReply(post.Id, c);
    setText("");
    setShowReply(false);
    try {
      setChildren(await loadChildren(post.Id));
      setOpen(true);
    } catch {
      /* mantém estado atual */
    }
  }

  return (
    <article className="rounded-lg bg-charcoal/60 border border-smoke px-3 py-3">
      <div className="flex gap-3">
        <Avatar name={post.AuthorName} src={post.AutorAvatarUrl} size={36} />
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 flex-wrap">
            <span className="font-semibold text-cream">{post.AuthorName}</span>
            {currentUserId && post.AutorId === currentUserId && (
              <span className="text-xs text-esmeralda">você</span>
            )}
            <span className="text-xs text-silver">· {timeAgo(post.CreatedAt)}</span>
          </div>
          <p className="mt-1 text-sm text-cream/90 whitespace-pre-wrap break-words">
            {post.Content}
          </p>

          <div className="mt-2 flex items-center gap-4 text-xs">
            {canReply && (
              <button
                type="button"
                onClick={() => setShowReply((s) => !s)}
                className="text-silver hover:text-esmeralda"
              >
                Responder
              </button>
            )}
            {showRepliesToggle && (
              <button
                type="button"
                onClick={toggleChildren}
                className="text-silver hover:text-cream"
              >
                {open ? "Ocultar respostas" : repliesLabel}
              </button>
            )}

            {viewerId ? (
              <>
                <button
                  type="button"
                  onClick={toggleLike}
                  className={`hover:text-rosa transition ${
                    liked ? "text-rosa" : "text-silver"
                  }`}
                >
                  {liked ? "❤️" : "🤍"} {likeCount > 0 ? likeCount : "Curtir"}
                </button>
                <button
                  type="button"
                  onClick={toggleSave}
                  className={`hover:text-amber transition ${
                    saved ? "text-amber" : "text-silver"
                  }`}
                >
                  {saved ? "🔖 Salvo" : "🔖 Salvar"}
                </button>
                <ShareButton
                  url={shareUrl}
                  label=""
                  className="text-silver hover:text-esmeralda"
                />
              </>
            ) : (
              likeCount > 0 && (
                <Link to="/login" className="text-silver hover:text-rosa">
                  ❤️ {likeCount}
                </Link>
              )
            )}

            <ReportButton
              targetType={reportTarget ?? "Post"}
              targetId={post.Id}
              ownContent={currentUserId === post.AutorId}
            />
          </div>

          {showReply && canReply && (
            <div className="mt-3">
              <textarea
                value={text}
                onChange={(e) => setText(e.target.value)}
                rows={2}
                placeholder="Escreva uma resposta…"
                className="w-full bg-smoke text-cream rounded-lg border border-smoke focus:border-esmeralda px-3 py-2 outline-none text-sm"
              />
              <div className="mt-2 flex gap-2">
                <button
                  type="button"
                  onClick={submit}
                  disabled={submitting || !text.trim()}
                  className="bg-brand text-ink text-sm font-semibold px-3 py-1.5 rounded-lg disabled:opacity-60"
                >
                  {submitting ? "Enviando…" : "Enviar"}
                </button>
                <button
                  type="button"
                  onClick={() => setShowReply(false)}
                  className="text-silver hover:text-cream text-sm px-2 py-1.5"
                >
                  Cancelar
                </button>
              </div>
            </div>
          )}

          {open && (
            <div className="mt-3 ml-1 border-l-2 border-smoke pl-4 space-y-2">
              {loadingKids ? (
                <p className="text-sm text-silver">Carregando…</p>
              ) : children && children.length > 0 ? (
                children.map((c) => (
                  <PostItem
                    key={c.Id}
                    post={c}
                    onReply={onReply}
                    loadChildren={loadChildren}
                    canPost={canPost}
                    currentUserId={currentUserId}
                    loadingId={loadingId}
                    reportTarget={reportTarget}
                    viewerId={viewerId}
                    shareUrl={shareUrl}
                  />
                ))
              ) : (
                <p className="text-sm text-silver">Sem respostas ainda.</p>
              )}
            </div>
          )}
        </div>
      </div>
    </article>
  );
}
