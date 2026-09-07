import { useState } from "react";
import { Avatar } from "./Avatar";
import { ReportButton } from "./ReportButton";
import { timeAgo } from "../lib/time";
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
    <div className="space-y-4">
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
}: ItemProps) {
  const [showReply, setShowReply] = useState(false);
  const [text, setText] = useState("");
  const [open, setOpen] = useState(false);
  const [children, setChildren] = useState<Post[] | null>(null);
  const [loadingKids, setLoadingKids] = useState(false);

  const canReply = canPost && post.Depth < MAX_DEPTH;
  const submitting = loadingId === post.Id;
  const childCount = post.ChildrenCount ?? 0;
  const knownZero = post.ChildrenCount === 0;
  const showRepliesToggle =
    !knownZero || open || (children != null && children.length > 0);
  const isRoot = post.Depth === 0;

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

  // Resposta (depth > 0): compacta, indentada — o palco é do card raiz.
  if (!isRoot) {
    return (
      <article className="rounded-xl bg-smoke/40 border border-smoke px-3 py-3">
        <div className="flex gap-3">
          <Avatar name={post.AuthorName} src={post.AutorAvatarUrl} size={28} />
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 flex-wrap">
              <span className="text-sm font-semibold text-cream">
                {post.AuthorName}
              </span>
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
              <ReportButton
                targetType={reportTarget ?? "Post"}
                targetId={post.Id}
                ownContent={currentUserId === post.AutorId}
              />
            </div>

            {showReply && canReply && <ReplyForm />}

            {open && <ChildrenArea />}
          </div>
        </div>
      </article>
    );
  }

  // Raiz (depth 0): card de publicação — cabeçalho com autor, estatísticas
  // reais (respostas) e barra de ações, no padrão do feed social.
  return (
    <article className="overflow-hidden rounded-2xl border border-smoke bg-charcoal">
      <div className="grid grid-cols-[minmax(0,1fr)_auto] items-start gap-3 p-4">
        <div className="flex min-w-0 items-center gap-3">
          <Avatar name={post.AuthorName} src={post.AutorAvatarUrl} size={40} />
          <div className="min-w-0">
            <p className="truncate text-sm font-semibold text-cream">
              {post.AuthorName}
              {currentUserId && post.AutorId === currentUserId && (
                <span className="ml-1.5 text-xs font-normal text-esmeralda">
                  você
                </span>
              )}
            </p>
            <p className="truncate text-xs text-silver">{timeAgo(post.CreatedAt)}</p>
          </div>
        </div>
        <ReportButton
          targetType={reportTarget ?? "Post"}
          targetId={post.Id}
          ownContent={currentUserId === post.AutorId}
        />
      </div>

      <p className="px-4 pb-4 text-sm leading-relaxed text-cream/90 whitespace-pre-wrap break-words">
        {post.Content}
      </p>

      {(childCount > 0 || showRepliesToggle) && (
        <div className="flex items-center px-4 py-2.5 text-xs text-silver border-t border-smoke">
          <span>
            {childCount} resposta{childCount === 1 ? "" : "s"}
          </span>
        </div>
      )}

      <div className="flex items-center gap-1 border-t border-smoke px-2 py-1.5">
        {canReply && (
          <button
            type="button"
            onClick={() => setShowReply((s) => !s)}
            className="flex flex-1 items-center justify-center gap-2 rounded-lg py-2 text-xs font-medium text-silver transition-colors hover:bg-smoke/60 hover:text-cream sm:text-sm"
          >
            💬 Responder
          </button>
        )}
        {showRepliesToggle && (
          <button
            type="button"
            onClick={toggleChildren}
            className="flex flex-1 items-center justify-center gap-2 rounded-lg py-2 text-xs font-medium text-silver transition-colors hover:bg-smoke/60 hover:text-cream sm:text-sm"
          >
            {open ? "Ocultar respostas" : `Ver ${childCount || ""} resposta${childCount === 1 ? "" : "s"}`}
          </button>
        )}
      </div>

      {(showReply && canReply) && (
        <div className="px-4 pb-4">
          <ReplyForm />
        </div>
      )}

      {open && (
        <div className="px-4 pb-4">
          <ChildrenArea />
        </div>
      )}
    </article>
  );

  function ReplyForm() {
    return (
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
    );
  }

  function ChildrenArea() {
    return (
      <div className="mt-3 ml-1 border-l-2 border-smoke pl-3 space-y-2">
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
            />
          ))
        ) : (
          <p className="text-sm text-silver">Sem respostas ainda.</p>
        )}
      </div>
    );
  }
}
