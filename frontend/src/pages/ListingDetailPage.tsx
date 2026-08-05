import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { ApiError, api } from "../api/client";
import { Badge } from "../components/Badge";
import { Avatar } from "../components/Avatar";
import { PostThread } from "../components/PostThread";
import { KIND_LABELS, MODO_META } from "../lib/config";
import { useAuth, type AuthUser } from "../auth/AuthContext";
import type { Comment, HelpRequest, Listing, ReportReason, Review } from "../api/types";

export function ListingDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();
  const [listing, setListing] = useState<Listing | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeImg, setActiveImg] = useState(0);

  // Comentários (thread recursiva — reuso do <PostThread>).
  const [comments, setComments] = useState<Comment[]>([]);
  const [commentBox, setCommentBox] = useState("");
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    if (!id) return;
    let active = true;
    setLoading(true);
    setError(null);
    api
      .listing(id)
      .then((d) => {
        if (active) {
          setListing(d);
          setActiveImg(0);
        }
      })
      .catch((e) => {
        if (active)
          setError(e instanceof ApiError ? e.message : "Anúncio não encontrado.");
      })
      .finally(() => {
        if (active) setLoading(false);
      });

    api.listingComments(id).then((c) => { if (active) setComments(c); }).catch(() => {});

    return () => {
      active = false;
    };
  }, [id]);

  async function submitRootComment() {
    if (!id || !commentBox.trim()) return;
    setSubmitting(true);
    try {
      await api.createComment(id, null, commentBox.trim());
      setCommentBox("");
      setComments(await api.listingComments(id));
    } catch {
      /* ignora — feedback opcional */
    } finally {
      setSubmitting(false);
    }
  }

  const canComment = !!user?.verified;

  if (loading)
    return <div className="app-container text-silver">Carregando…</div>;

  if (error || !listing)
    return (
      <div className="app-container text-center">
        <p className="text-silver mb-4">{error ?? "Anúncio não encontrado."}</p>
        <Link to="/feed" className="text-esmeralda hover:underline">
          ← Voltar ao feed
        </Link>
      </div>
    );

  const gratis = listing.PrecoRvm === 0;
  const images = listing.Imagens?.length ? listing.Imagens : [];

  return (
    <div className="app-container">
      <Link to="/feed" className="text-sm text-silver hover:text-cream mb-4 inline-block">
        ← Feed
      </Link>

      <div className="grid lg:grid-cols-2 gap-8 lg:gap-12">
        {/* Galeria */}
        <div>
          <div className="aspect-square bg-smoke rounded-xl border border-smoke overflow-hidden flex items-center justify-center text-7xl">
            {images[activeImg] ? (
              <img
                src={images[activeImg]}
                alt={listing.Titulo}
                className="w-full h-full object-cover"
              />
            ) : (
              <span aria-hidden>📦</span>
            )}
          </div>
          {images.length > 1 && (
            <div className="mt-3 flex gap-2 overflow-x-auto">
              {images.map((src, i) => (
                <button
                  key={i}
                  onClick={() => setActiveImg(i)}
                  className={`w-16 h-16 shrink-0 rounded-lg overflow-hidden border-2 ${
                    i === activeImg ? "border-esmeralda" : "border-smoke"
                  }`}
                  aria-label={`Imagem ${i + 1}`}
                >
                  <img src={src} alt="" className="w-full h-full object-cover" />
                </button>
              ))}
            </div>
          )}
        </div>

        {/* Info */}
        <div>
          <div className="flex items-center gap-2 mb-3">
            <Badge modo={listing.Modo} size="md" />
            <span className="text-sm text-silver">{KIND_LABELS[listing.Kind]}</span>
            {listing.NftTokenId && (
              <span className="text-xs text-silver border border-smoke rounded-full px-2 py-0.5">
                NFT #{listing.NftTokenId}
              </span>
            )}
          </div>

          <h1 className="text-3xl font-bold text-cream">{listing.Titulo}</h1>
          <div className="mt-3 text-2xl">
            {gratis ? (
              <span className="rms text-lima">Grátis</span>
            ) : (
              <span className="rms text-cream">
                RM$ {listing.PrecoRvm.toLocaleString("pt-BR")}
              </span>
            )}
          </div>

          <p className="mt-4 text-cream/90 whitespace-pre-wrap">{listing.Descricao}</p>

          <dl className="mt-6 space-y-2 text-sm">
            {listing.Kind === "Product" && listing.ProductDetails && (
              <div className="flex gap-2">
                <dt className="text-silver w-28">Condição:</dt>
                <dd className="text-cream">
                  {listing.ProductDetails.Condition}
                  {listing.ProductDetails.Stock != null
                    ? ` · ${listing.ProductDetails.Stock} un.`
                    : ""}
                </dd>
              </div>
            )}
            {listing.Kind === "Service" && listing.ServiceDetails && (
              <>
                <div className="flex gap-2">
                  <dt className="text-silver w-28">Unidade:</dt>
                  <dd className="text-cream">{listing.ServiceDetails.UnitType}</dd>
                </div>
                {listing.ServiceDetails.Duration != null && (
                  <div className="flex gap-2">
                    <dt className="text-silver w-28">Duração:</dt>
                    <dd className="text-cream">{listing.ServiceDetails.Duration}</dd>
                  </div>
                )}
                {listing.ServiceDetails.VoucherExpiryDays != null && (
                  <div className="flex gap-2">
                    <dt className="text-silver w-28">Voucher:</dt>
                    <dd className="text-cream">
                      {listing.ServiceDetails.VoucherExpiryDays} dias
                    </dd>
                  </div>
                )}
              </>
            )}
            {(listing.Cidade || listing.Bairro) && (
              <div className="flex gap-2">
                <dt className="text-silver w-28">Local:</dt>
                <dd className="text-cream">
                  {[listing.Bairro, listing.Cidade].filter(Boolean).join(", ")}
                </dd>
              </div>
            )}
          </dl>

          {/* Vendedor */}
          <div className="mt-6 flex items-center gap-3 bg-smoke rounded-xl p-3">
            {listing.VendedorAvatarUrl ? (
              <img
                src={listing.VendedorAvatarUrl}
                alt=""
                className="w-10 h-10 rounded-full"
              />
            ) : (
              <div className="w-10 h-10 rounded-full bg-charcoal border border-smoke" />
            )}
            <div>
              <div className="text-cream font-medium">{listing.VendedorNome}</div>
              <div className="text-xs text-silver">
                {MODO_META[listing.Modo].emoji} {MODO_META[listing.Modo].label}
              </div>
            </div>
          </div>

          {/* Avaliações do vendedor (recebidas em trocas concluídas — UF-23) */}
          <SellerReviews vendedorId={listing.VendedorId} />

          {/* Comparativo de preço — placeholder Fase 3 */}
          <div className="mt-6 border border-dashed border-smoke rounded-xl p-4 text-sm text-silver">
            <span className="text-amber">⌖</span> Comparativo de preços em breve (Fase 3).
          </div>

          {/* CTA */}
          <div className="mt-6">
            <ListingActions listing={listing} user={user} />
          </div>

          {/* Denunciar anúncio (UF-24) */}
          <ReportListing listingId={listing.Id} user={user} />
        </div>
      </div>

      {/* Comentários — thread recursiva reutilizando o <PostThread> */}
      <section className="mt-10 lg:mt-14">
        <h2 className="text-xl font-bold text-cream mb-4">
          Comentários
          {comments.length > 0 && (
            <span className="text-silver text-sm font-normal"> · {comments.length}</span>
          )}
        </h2>

        {canComment ? (
          <div className="mb-5">
            <textarea
              value={commentBox}
              onChange={(e) => setCommentBox(e.target.value)}
              rows={2}
              placeholder="Pergunte ou comente sobre este anúncio…"
              className="w-full bg-smoke text-cream rounded-lg border border-smoke focus:border-esmeralda px-4 py-2.5 outline-none text-sm"
            />
            <button
              type="button"
              onClick={submitRootComment}
              disabled={submitting || !commentBox.trim()}
              className="mt-2 bg-brand text-ink text-sm font-semibold px-4 py-2 rounded-lg disabled:opacity-60"
            >
              {submitting ? "Enviando…" : "Comentar"}
            </button>
          </div>
        ) : (
          <p className="text-sm text-silver mb-5">
            {user
              ? "Confirme seu e-mail e telefone para comentar."
              : (
                <>
                  <Link to="/login" className="text-esmeralda hover:underline">Entre</Link> para comentar.
                </>
              )}
          </p>
        )}

        {comments.length === 0 ? (
          <p className="text-silver text-sm">Nenhum comentário ainda. Seja o primeiro!</p>
        ) : (
          <PostThread
            posts={comments}
            onReply={async (parentId, conteudo) => {
              await api.createComment(id!, parentId, conteudo);
            }}
            loadChildren={(parentId) => api.listingComments(id!, parentId)}
            canPost={canComment}
            currentUserId={user?.userId}
          />
        )}
      </section>
    </div>
  );
}

function timeAgo(iso: string): string {
  const diff = Date.now() - new Date(iso).getTime();
  if (!Number.isFinite(diff) || diff < 0) return "agora";
  const min = Math.floor(diff / 60000);
  if (min < 1) return "agora";
  if (min < 60) return `${min} min`;
  const h = Math.floor(min / 60);
  if (h < 24) return `${h} h`;
  const d = Math.floor(h / 24);
  return d === 1 ? "1 dia" : `${d} dias`;
}

function VerifyHint({ action }: { action: string }) {
  return (
    <div className="bg-charcoal border border-smoke rounded-xl p-4 text-sm text-silver text-center">
      Confirme seu e-mail e telefone para {action}.
    </div>
  );
}

// Renderiza n/5 estrelas (somente leitura).
function Stars({ n }: { n: number }) {
  return (
    <span className="text-amber text-sm tracking-tight" aria-label={`${n} de 5 estrelas`}>
      {"★".repeat(Math.max(0, Math.min(5, n)))}
      <span className="text-smoke">{"★".repeat(Math.max(0, 5 - Math.max(0, Math.min(5, n))))}</span>
    </span>
  );
}

// Avaliações recebidas pelo vendedor (trocas concluídas). Leitura pública (UF-23).
function SellerReviews({ vendedorId }: { vendedorId: string }) {
  const [reviews, setReviews] = useState<Review[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;
    setLoading(true);
    api
      .userReviews(vendedorId, 5)
      .then((r) => {
        if (active) setReviews(r);
      })
      .catch(() => {})
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, [vendedorId]);

  if (loading) return null;
  if (reviews.length === 0) return null;

  const avg =
    reviews.reduce((s, r) => s + r.Rating, 0) / reviews.length;

  return (
    <div className="mt-6 border border-smoke rounded-xl p-4">
      <h3 className="text-cream font-semibold mb-1 flex items-center gap-2">
        Avaliações do vendedor
        <span className="text-xs text-silver font-normal">
          · média <Stars n={Math.round(avg)} /> ({reviews.length})
        </span>
      </h3>
      <ul className="mt-3 space-y-3">
        {reviews.map((r) => (
          <li key={r.Id} className="text-sm">
            <div className="flex items-center gap-2 mb-0.5">
              <span className="text-cream font-medium">{r.ReviewerNome}</span>
              <Stars n={r.Rating} />
              <span className="text-xs text-silver ml-auto">{timeAgo(r.CreatedAt)}</span>
            </div>
            {r.Comment && (
              <p className="text-cream/80 whitespace-pre-wrap">{r.Comment}</p>
            )}
          </li>
        ))}
      </ul>
    </div>
  );
}

function ListingActions({ listing, user }: { listing: Listing; user: AuthUser | null }) {
  const isOwner = !!user && user.userId === listing.VendedorId;
  const isDonation = listing.Modo === "Doar" || listing.Modo === "Voluntariar";
  const isService = listing.Kind === "Service";
  const buyLabel = isService ? "Contratar" : "Comprar/Trocar";

  const [purchasing, setPurchasing] = useState(false);
  const [purchaseErr, setPurchaseErr] = useState<string | null>(null);
  const [purchased, setPurchased] = useState(false);

  const [showHelp, setShowHelp] = useState(false);
  const [msg, setMsg] = useState("");
  const [sendingHelp, setSendingHelp] = useState(false);
  const [helpErr, setHelpErr] = useState<string | null>(null);
  const [helpSent, setHelpSent] = useState(false);

  const [queue, setQueue] = useState<HelpRequest[]>([]);
  const [queueLoading, setQueueLoading] = useState(false);
  const [selecting, setSelecting] = useState<string | null>(null);
  const [queueErr, setQueueErr] = useState<string | null>(null);
  const [queueOk, setQueueOk] = useState<string | null>(null);

  useEffect(() => {
    if (!isOwner || !isDonation) return;
    let active = true;
    setQueueLoading(true);
    api
      .helpQueue(listing.Id)
      .then((q) => {
        if (active) setQueue(q);
      })
      .catch(() => {})
      .finally(() => {
        if (active) setQueueLoading(false);
      });
    return () => {
      active = false;
    };
  }, [isOwner, isDonation, listing.Id]);

  async function doPurchase() {
    setPurchasing(true);
    setPurchaseErr(null);
    try {
      await api.purchase(listing.Id);
      setPurchased(true);
    } catch (e) {
      setPurchaseErr(
        e instanceof ApiError ? e.message : "Não foi possível iniciar a troca."
      );
    } finally {
      setPurchasing(false);
    }
  }

  async function doRequest() {
    if (!msg.trim()) return;
    setSendingHelp(true);
    setHelpErr(null);
    try {
      await api.requestHelp(listing.Id, msg.trim());
      setHelpSent(true);
      setMsg("");
    } catch (e) {
      setHelpErr(
        e instanceof ApiError ? e.message : "Não foi possível enviar o pedido."
      );
    } finally {
      setSendingHelp(false);
    }
  }

  async function doSelect(req: HelpRequest) {
    setSelecting(req.Id);
    setQueueErr(null);
    setQueueOk(null);
    try {
      await api.selectRecipient(req.Id);
      setQueueOk(`Receptor escolhido: ${req.AuthorNome}`);
      setQueue(await api.helpQueue(listing.Id));
    } catch (e) {
      setQueueErr(
        e instanceof ApiError ? e.message : "Não foi possível selecionar o receptor."
      );
    } finally {
      setSelecting(null);
    }
  }

  if (isDonation) {
    if (isOwner) {
      return (
        <div className="space-y-4">
          <div className="bg-charcoal border border-smoke rounded-xl p-4 text-sm text-silver">
            👋 Este é o seu anúncio. Aqui ficam os pedidos de quem precisa.
          </div>
          <div>
            <h3 className="text-cream font-semibold mb-2">Fila de pedidos</h3>
            {queueErr && <p className="text-rosa text-sm mb-2">{queueErr}</p>}
            {queueOk && (
              <p className="text-esmeralda text-sm mb-2">
                ✓ {queueOk} ·{" "}
                <Link to="/trades" className="underline">
                  Ver no tracker →
                </Link>
              </p>
            )}
            {queueLoading ? (
              <p className="text-silver text-sm">Carregando pedidos…</p>
            ) : queue.length === 0 ? (
              <p className="text-silver text-sm">Nenhum pedido ainda.</p>
            ) : (
              <ul className="space-y-2">
                {queue.map((req) => (
                  <li key={req.Id} className="bg-smoke rounded-lg p-3">
                    <div className="flex items-center gap-2 mb-1">
                      <Avatar
                        name={req.AuthorNome}
                        src={req.AuthorAvatarUrl}
                        size={28}
                      />
                      <span className="text-cream text-sm font-medium">
                        {req.AuthorNome}
                      </span>
                      <span className="text-silver text-xs ml-auto">
                        {timeAgo(req.CreatedAt)}
                      </span>
                    </div>
                    <p className="text-cream/90 text-sm whitespace-pre-wrap">
                      {req.Mensagem}
                    </p>
                    {req.SelectedTradeId ? (
                      <span className="inline-block mt-2 text-xs text-esmeralda">
                        ✓ Selecionado
                      </span>
                    ) : (
                      <button
                        type="button"
                        onClick={() => doSelect(req)}
                        disabled={selecting !== null}
                        className="mt-2 bg-brand text-ink text-xs font-semibold px-3 py-1.5 rounded-lg disabled:opacity-60"
                      >
                        {selecting === req.Id
                          ? "Selecionando…"
                          : `Selecionar ${req.AuthorNome.split(" ")[0]}`}
                      </button>
                    )}
                  </li>
                ))}
              </ul>
            )}
          </div>
        </div>
      );
    }

    if (!user) {
      return (
        <Link
          to="/login"
          className="block text-center bg-help text-ink font-semibold px-6 py-3 rounded-xl"
        >
          Entre para pedir
        </Link>
      );
    }
    if (!user.verified) return <VerifyHint action="pedir" />;
    if (helpSent) {
      return (
        <div className="bg-esmeralda/10 border border-esmeralda/30 rounded-xl p-4 text-sm text-cream">
          ✓ Pedido enviado! O doador vai avaliar.
        </div>
      );
    }
    return (
      <div>
        {helpErr && <p className="text-rosa text-sm mb-2">{helpErr}</p>}
        {showHelp ? (
          <div className="space-y-2">
            <textarea
              value={msg}
              onChange={(e) => setMsg(e.target.value)}
              rows={3}
              placeholder="Conte por que você precisa, em poucas palavras…"
              className="w-full bg-smoke text-cream rounded-lg border border-smoke focus:border-esmeralda px-4 py-2.5 outline-none text-sm"
            />
            <div className="flex gap-2">
              <button
                type="button"
                onClick={doRequest}
                disabled={sendingHelp || !msg.trim()}
                className="bg-help text-ink text-sm font-semibold px-4 py-2 rounded-lg disabled:opacity-60"
              >
                {sendingHelp ? "Enviando…" : "Enviar pedido"}
              </button>
              <button
                type="button"
                onClick={() => {
                  setShowHelp(false);
                  setMsg("");
                  setHelpErr(null);
                }}
                className="text-silver text-sm px-3 py-2 hover:text-cream"
              >
                Cancelar
              </button>
            </div>
          </div>
        ) : (
          <button
            type="button"
            onClick={() => setShowHelp(true)}
            className="w-full bg-help text-ink font-semibold px-6 py-3 rounded-xl"
          >
            Pedir
          </button>
        )}
      </div>
    );
  }

  if (isOwner) {
    return (
      <div className="bg-charcoal border border-smoke rounded-xl p-4 text-sm text-silver text-center">
        Este é o seu anúncio. 🌱
      </div>
    );
  }
  if (!user) {
    return (
      <Link
        to="/login"
        className="block text-center bg-brand text-ink font-semibold px-6 py-3 rounded-xl"
      >
        Entre para {isService ? "contratar" : "comprar/trocar"}
      </Link>
    );
  }
  if (!user.verified) {
    return <VerifyHint action={isService ? "contratar" : "comprar"} />;
  }
  if (purchased) {
    return (
      <div className="bg-esmeralda/10 border border-esmeralda/30 rounded-xl p-4 text-sm text-cream">
        ✓ Troca iniciada!{" "}
        <Link to="/trades" className="underline font-medium">
          Ver no tracker →
        </Link>
      </div>
    );
  }
  return (
    <div>
      {purchaseErr && <p className="text-rosa text-sm mb-2">{purchaseErr}</p>}
      <button
        type="button"
        onClick={doPurchase}
        disabled={purchasing}
        className="w-full bg-brand text-ink font-semibold px-6 py-3 rounded-xl disabled:opacity-60"
      >
        {purchasing ? "Processando…" : buyLabel}
      </button>
    </div>
  );
}

// Denúncia de anúncio (UF-24). Só aparece para usuários verificados que não são o dono.
// Motivo (Spam/Inadequado/Golpe/Outro) + detalhes opcional → api.createReport("Listing", ...).
function ReportListing({ listingId, user }: { listingId: string; user: AuthUser | null }) {
  const [open, setOpen] = useState(false);
  const [reason, setReason] = useState<ReportReason>("Spam");
  const [details, setDetails] = useState("");
  const [sending, setSending] = useState(false);
  const [err, setErr] = useState<string | null>(null);
  const [sent, setSent] = useState(false);

  if (!user || !user.verified) return null;

  async function submit() {
    setSending(true);
    setErr(null);
    try {
      await api.createReport("Listing", listingId, reason, details.trim() || undefined);
      setSent(true);
      setOpen(false);
      setDetails("");
    } catch (e) {
      setErr(e instanceof ApiError ? e.message : "Não foi possível enviar a denúncia.");
    } finally {
      setSending(false);
    }
  }

  if (sent) {
    return (
      <p className="mt-4 text-sm text-esmeralda">
        ✓ Denúncia enviada — obrigado.
      </p>
    );
  }

  return (
    <div className="mt-4">
      {err && <p className="text-rosa text-sm mb-2">{err}</p>}
      {open ? (
        <div className="border border-smoke rounded-xl p-4 space-y-3">
          <label className="block text-sm text-silver">Motivo da denúncia</label>
          <select
            value={reason}
            onChange={(e) => setReason(e.target.value as ReportReason)}
            className="w-full bg-smoke text-cream rounded-lg border border-smoke focus:border-esmeralda px-3 py-2 outline-none text-sm"
          >
            <option value="Spam">Spam</option>
            <option value="Inappropriate">Inadequado</option>
            <option value="Scam">Golpe</option>
            <option value="Other">Outro</option>
          </select>
          <textarea
            value={details}
            onChange={(e) => setDetails(e.target.value)}
            rows={2}
            placeholder="Detalhe o problema (opcional)…"
            className="w-full bg-smoke text-cream rounded-lg border border-smoke focus:border-esmeralda px-4 py-2.5 outline-none text-sm"
          />
          <div className="flex gap-2">
            <button
              type="button"
              onClick={submit}
              disabled={sending}
              className="bg-rosa/90 text-ink text-sm font-semibold px-4 py-2 rounded-lg disabled:opacity-60"
            >
              {sending ? "Enviando…" : "Enviar denúncia"}
            </button>
            <button
              type="button"
              onClick={() => {
                setOpen(false);
                setDetails("");
                setErr(null);
              }}
              className="text-silver text-sm px-3 py-2 hover:text-cream"
            >
              Cancelar
            </button>
          </div>
        </div>
      ) : (
        <button
          type="button"
          onClick={() => setOpen(true)}
          className="text-sm text-silver hover:text-rosa underline underline-offset-4"
        >
          ⚑ Denunciar anúncio
        </button>
      )}
    </div>
  );
}
