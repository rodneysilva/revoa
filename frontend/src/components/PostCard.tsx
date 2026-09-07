import { useState } from "react";
import { Link } from "react-router-dom";
import { Bookmark, Heart, MessageCircle, MessagesSquare } from "lucide-react";
import { Avatar } from "./Avatar";
import { ShareButton } from "./ShareButton";
import { timeAgo } from "../lib/time";
import { api } from "../api/client";
import type { Post } from "../api/types";

// Card de post do feed unificado: mesma moldura do ListingTimelineCard
// (bg-charcoal border-smoke rounded-xl) para posts e anúncios não destoarem.
// Like/salvar otimistas com rollback; anônimo vê link para /login.
export function PostCard({
  post,
  communityName,
  communityUrl,
  communityCover,
  currentUserId,
  onUnsave,
}: {
  post: Post;
  communityName?: string;
  communityUrl?: string;
  // Miniatura da capa ao lado do nome da comunidade (feed da rede).
  communityCover?: string;
  currentUserId?: string;
  // "Meus salvos": avisa a página quando o post deixa de estar salvo (a lista remove).
  onUnsave?: () => void;
}) {
  const [liked, setLiked] = useState(post.IsLiked ?? false);
  const [likeCount, setLikeCount] = useState(post.LikeCount ?? 0);
  const [saved, setSaved] = useState(post.IsSaved ?? false);
  const [busy, setBusy] = useState<"like" | "save" | null>(null);

  const communityHref = communityUrl ?? `/community/${post.CommunityId}#conversas`;
  const shareUrl = `${window.location.origin}${communityUrl ?? `/community/${post.CommunityId}#conversas`}`;

  async function toggleLike() {
    if (!currentUserId || busy) return;
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
    if (!currentUserId || busy) return;
    setBusy("save");
    const next = !saved;
    setSaved(next);
    try {
      const on = await api.savePost(post.Id);
      if (on !== next) setSaved(on);
      if (!on) onUnsave?.();
    } catch {
      setSaved(!next);
    } finally {
      setBusy(null);
    }
  }

  const likeButton = currentUserId ? (
    <button
      type="button"
      onClick={toggleLike}
      className={`inline-flex items-center gap-1 text-xs hover:text-rosa transition ${
        liked ? "text-rosa" : "text-silver"
      }`}
    >
      <Heart aria-hidden className={`w-3.5 h-3.5 ${liked ? "fill-current" : ""}`} />
      <span>{likeCount > 0 ? likeCount : "Curtir"}</span>
    </button>
  ) : (
    <Link
      to="/login"
      className="inline-flex items-center gap-1 text-xs text-silver hover:text-rosa"
    >
      <Heart aria-hidden className="w-3.5 h-3.5" />
      <span>{likeCount > 0 ? likeCount : "Curtir"}</span>
    </Link>
  );

  const saveButton = currentUserId ? (
    <button
      type="button"
      onClick={toggleSave}
      className={`inline-flex items-center gap-1 text-xs hover:text-amber transition ${
        saved ? "text-amber" : "text-silver"
      }`}
    >
      <Bookmark aria-hidden className={`w-3.5 h-3.5 ${saved ? "fill-current" : ""}`} />
      <span>{saved ? "Salvo" : "Salvar"}</span>
    </button>
  ) : (
    <Link
      to="/login"
      className="inline-flex items-center gap-1 text-xs text-silver hover:text-amber"
    >
      <Bookmark aria-hidden className="w-3.5 h-3.5" />
      <span>Salvar</span>
    </Link>
  );

  return (
    <article className="rounded-xl bg-charcoal border border-smoke p-4 hover:border-smoke transition">
      <div className="flex gap-3">
        <Avatar name={post.AuthorName} src={post.AutorAvatarUrl} size={40} />
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 flex-wrap">
            <span className="font-semibold text-cream">{post.AuthorName}</span>
            {currentUserId && post.AutorId === currentUserId && (
              <span className="text-xs text-esmeralda">você</span>
            )}
            <span className="text-xs text-silver">· {timeAgo(post.CreatedAt)}</span>
            {communityName && (
              <Link
                to={communityHref}
                className="ml-auto inline-flex items-center gap-1.5 text-xs text-amber hover:underline truncate"
              >
                {communityCover ? (
                  <img
                    src={communityCover}
                    alt=""
                    className="w-5 h-5 rounded object-cover shrink-0"
                    loading="lazy"
                    onError={(e) => {
                      (e.target as HTMLImageElement).style.display = "none";
                    }}
                  />
                ) : (
                  <MessagesSquare aria-hidden className="w-4 h-4" />
                )}
                <span className="truncate">{communityName}</span>
              </Link>
            )}
          </div>
          <p className="mt-1 text-sm text-cream/90 whitespace-pre-wrap break-words line-clamp-6">
            {post.Content}
          </p>
          <div className="mt-2.5 flex flex-wrap items-center gap-x-4 gap-y-1">
            {likeButton}
            {saveButton}
            <Link
              to={communityHref}
              className="inline-flex items-center gap-1 text-xs text-silver hover:text-esmeralda transition"
            >
              <MessageCircle aria-hidden className="w-3.5 h-3.5" />
              {(post.ChildrenCount ?? 0) > 0
                ? `${post.ChildrenCount} resposta${post.ChildrenCount === 1 ? "" : "s"}`
                : "Conversar"}
            </Link>
            <ShareButton url={shareUrl} label="" className="text-xs text-silver hover:text-esmeralda" />
          </div>
        </div>
      </div>
    </article>
  );
}
