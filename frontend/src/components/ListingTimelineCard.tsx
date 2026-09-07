import { useState } from "react";
import { Link } from "react-router-dom";
import { Avatar } from "./Avatar";
import { Badge } from "./Badge";
import { PostThread } from "./PostThread";
import { ShareButton } from "./ShareButton";
import { brlEstimate } from "../lib/format";
import { timeAgo } from "../lib/time";
import { useBrlRate } from "../lib/useBrlRate";
import { api } from "../api/client";
import type { Comment, FeedItem } from "../api/types";

// Anúncio na timeline: a MESMA estrutura do PostCard (avatar + nome + tempo,
// action bar na moldura bg-charcoal border-smoke). Conteúdo: foto fixa à
// esquerda (tamanho por resolução) + título/descrição/preço à direita.
//
// `interactive` (timeline da comunidade): o anúncio se comporta como um post —
// curtir, comentar inline e salvar exigem canInteract (membro verificado);
// sem vínculo a comunidade aparece normal, só não interage (interactHint
// explica). Nas outras telas (/feed) o card fica só com
// salvar/compartilhar/ver, sem gate de membership.
export function ListingTimelineCard({
  item,
  currentUserId,
  initialSaved,
  initialLiked,
  interactive,
  canInteract,
  interactHint,
  onUnsave,
}: {
  item: FeedItem;
  currentUserId?: string;
  initialSaved?: boolean;
  initialLiked?: boolean;
  interactive?: boolean;
  // Verdadeiro quando o usuário pode interagir (comunidade: membro verificado).
  canInteract?: boolean;
  // Por que não pode interagir (ex.: "Entre na comunidade para interagir.").
  interactHint?: string;
  // "Meus salvos": avisa a página quando o anúncio deixa de estar salvo.
  onUnsave?: () => void;
}) {
  const [saved, setSaved] = useState(initialSaved ?? false);
  const [liked, setLiked] = useState(initialLiked ?? false);
  const [busy, setBusy] = useState<"like" | "save" | null>(null);
  const [showComments, setShowComments] = useState(false);
  const [comments, setComments] = useState<Comment[] | null>(null);
  const [commentBox, setCommentBox] = useState("");
  const [sending, setSending] = useState(false);
  const { rate } = useBrlRate();

  const gratis = item.PriceRvm === 0;
  const brl = !gratis ? brlEstimate(item.PriceRvm, rate ?? 0) : null;
  const local =
    item.DistanciaKm != null
      ? `~${item.DistanciaKm.toFixed(1)} km`
      : item.Neighborhood || item.City || null;
  const detailUrl = `/listings/${item.Id}`;

  async function toggleSave() {
    if (!currentUserId || busy) return;
    setBusy("save");
    const next = !saved;
    setSaved(next);
    try {
      const on = await api.saveListing(item.Id);
      if (on !== next) setSaved(on);
      if (!on) onUnsave?.();
    } catch {
      setSaved(!next);
    } finally {
      setBusy(null);
    }
  }

  async function toggleLike() {
    if (!canInteract || busy) return;
    setBusy("like");
    const next = !liked;
    setLiked(next);
    try {
      const on = await api.likeListing(item.Id);
      if (on !== next) setLiked(on);
    } catch {
      setLiked(!next);
    } finally {
      setBusy(null);
    }
  }

  async function openComments() {
    const next = !showComments;
    setShowComments(next);
    if (next && comments === null) {
      try {
        setComments(await api.listingComments(item.Id));
      } catch {
        setComments([]);
      }
    }
  }

  async function submitComment() {
    if (!commentBox.trim() || sending) return;
    setSending(true);
    try {
      await api.createComment(item.Id, null, commentBox.trim());
      setCommentBox("");
      setComments(await api.listingComments(item.Id));
    } catch {
      /* mantém o texto para retentar */
    } finally {
      setSending(false);
    }
  }

  // Na comunidade, salvar também exige vínculo (interação como um post).
  const canSave = interactive ? !!canInteract : !!currentUserId;

  const likeButton = canInteract ? (
    <button
      type="button"
      onClick={toggleLike}
      className={`text-xs hover:text-rosa transition ${liked ? "text-rosa" : "text-silver"}`}
    >
      {liked ? "❤️ Curtido" : "🤍 Curtir"}
    </button>
  ) : interactive ? (
    <span className="text-xs text-silver" title={interactHint}>
      🤍 Curtir
    </span>
  ) : (
    <Link to="/login" className="text-xs text-silver hover:text-rosa">
      🤍 Curtir
    </Link>
  );

  const saveButton = canSave ? (
    <button
      type="button"
      onClick={toggleSave}
      className={`text-xs hover:text-amber transition ${saved ? "text-amber" : "text-silver"}`}
    >
      {saved ? "🔖 Salvo" : "🔖 Salvar"}
    </button>
  ) : interactive ? (
    <span className="text-xs text-silver" title={interactHint}>
      🔖 Salvar
    </span>
  ) : (
    <Link to="/login" className="text-xs text-silver hover:text-amber">
      🔖 Salvar
    </Link>
  );

  return (
    <article className="rounded-xl bg-charcoal border border-smoke p-4 transition">
      {/* Cabeçalho — igual ao PostCard: vendedor + tempo + badge do modo */}
      <div className="flex gap-3">
        <Avatar name={item.SellerName} src={item.SellerAvatarUrl} size={40} />
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 flex-wrap">
            <span className="font-semibold text-cream truncate">{item.SellerName}</span>
            <span className="text-xs text-silver whitespace-nowrap">
              · {item.CreatedAt ? timeAgo(item.CreatedAt) : ""}
              {local ? ` · 📍 ${local}` : ""}
            </span>
            <span className="ml-auto shrink-0">
              <Badge modo={item.Mode} />
            </span>
          </div>

          {/* Conteúdo: foto fixa à esquerda (por resolução) + texto à direita */}
          <div className="mt-2 flex gap-3">
            <Link
              to={detailUrl}
              className="shrink-0 w-24 h-24 sm:w-28 sm:h-28 xl:w-32 xl:h-32 rounded-lg overflow-hidden bg-smoke flex items-center justify-center text-3xl border border-smoke"
            >
              {item.PrimeiraImagem ? (
                <img
                  src={item.PrimeiraImagem}
                  alt={item.Title}
                  className="w-full h-full object-cover"
                  loading="lazy"
                  onError={(e) => {
                    (e.target as HTMLImageElement).style.display = "none";
                  }}
                />
              ) : (
                <span aria-hidden>{item.Kind === "Service" ? "🛠️" : "📦"}</span>
              )}
            </Link>

            <div className="flex-1 min-w-0">
              <Link to={detailUrl} className="block group">
                <h3 className="font-semibold text-cream line-clamp-1 group-hover:text-esmeralda">
                  {item.Kind === "Service" ? "🛠️ " : "📦 "}
                  {item.Title}
                </h3>
              </Link>

              {item.Description && (
                <p className="mt-1 text-sm text-cream/90 whitespace-pre-wrap break-words line-clamp-2">
                  {item.Description}
                </p>
              )}

              <div className="mt-2">
                {gratis ? (
                  <span className="rms text-lima">Grátis · RM$ 0</span>
                ) : (
                  <span className="rms text-cream">
                    RM$ {item.PriceRvm.toLocaleString("pt-BR")}
                    {brl && <span className="text-xs text-silver ml-1.5">≈ {brl}</span>}
                  </span>
                )}
              </div>
            </div>
          </div>

          {/* Action bar — mesma linha do PostCard */}
          <div className="mt-2.5 flex items-center gap-4 flex-wrap">
            {interactive && likeButton}
            {interactive && (
              <button
                type="button"
                onClick={openComments}
                className={`text-xs hover:text-esmeralda transition ${showComments ? "text-esmeralda" : "text-silver"}`}
              >
                {item.CommentCount && item.CommentCount > 0
                  ? `💬 ${item.CommentCount} comentário${item.CommentCount === 1 ? "" : "s"}`
                  : "💬 Comentar"}
              </button>
            )}
            {saveButton}
            <ShareButton
              url={`${window.location.origin}${detailUrl}`}
              label=""
              className="text-xs text-silver hover:text-esmeralda"
            />
            <Link
              to={detailUrl}
              className="ml-auto text-xs text-silver hover:text-esmeralda transition"
            >
              Ver anúncio →
            </Link>
          </div>

          {/* Comentários inline (só na timeline da comunidade) */}
          {interactive && showComments && (
            <div className="mt-3 border-t border-smoke pt-3 space-y-2">
              {canInteract ? (
                <div>
                  <textarea
                    value={commentBox}
                    onChange={(e) => setCommentBox(e.target.value)}
                    rows={2}
                    placeholder="Pergunte ou comente sobre este anúncio…"
                    className="w-full bg-smoke text-cream rounded-lg border border-smoke focus:border-esmeralda px-3 py-2 outline-none text-sm"
                  />
                  <button
                    type="button"
                    onClick={submitComment}
                    disabled={sending || !commentBox.trim()}
                    className="mt-1.5 bg-brand text-ink text-xs font-semibold px-3 py-1.5 rounded-lg disabled:opacity-60"
                  >
                    {sending ? "Enviando…" : "Comentar"}
                  </button>
                </div>
              ) : (
                <p className="text-xs text-silver">
                  {interactHint ?? (
                    <>
                      <Link to="/login" className="text-esmeralda hover:underline">
                        Entre
                      </Link>{" "}
                      para comentar.
                    </>
                  )}
                </p>
              )}

              {comments === null ? (
                <p className="text-xs text-silver">Carregando comentários…</p>
              ) : comments.length === 0 ? (
                <p className="text-xs text-silver">Nenhum comentário ainda.</p>
              ) : (
                <PostThread
                  posts={comments}
                  onReply={async (parentId, conteudo) => {
                    await api.createComment(item.Id, parentId, conteudo);
                  }}
                  loadChildren={async (parentId) => {
                    try {
                      return await api.listingComments(item.Id, parentId);
                    } catch {
                      return [];
                    }
                  }}
                  canPost={!!canInteract}
                  currentUserId={currentUserId}
                  reportTarget="Comment"
                />
              )}
            </div>
          )}
        </div>
      </div>
    </article>
  );
}
