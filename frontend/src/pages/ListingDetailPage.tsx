import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { ApiError, api } from "../api/client";
import { Badge } from "../components/Badge";
import { Avatar } from "../components/Avatar";
import { PostThread } from "../components/PostThread";
import { KIND_LABELS, MODO_META } from "../lib/config";
import { brlEstimate } from "../lib/format";
import { timeAgo } from "../lib/time";
import { useBrlRate } from "../lib/useBrlRate";
import { useAuth, type AuthUser } from "../auth/AuthContext";
import type { Comment, HelpRequest, Listing, PriceReference, ReportReason, Review } from "../api/types";

export function ListingDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();
  const { rate, disclaimer } = useBrlRate();
  const [listing, setListing] = useState<Listing | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeImg, setActiveImg] = useState(0);

  // Comentários (thread recursiva — reuso do <PostThread>).
  const [comments, setComments] = useState<Comment[]>([]);
  const [commentBox, setCommentBox] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [priceRef, setPriceRef] = useState<PriceReference | null>(null);

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
          // Comparativo: busca a referencia de preco da categoria do anuncio.
          if (d.CategoryId) {
            api.pricingByCategory(d.CategoryId)
              .then((ref) => { if (active) setPriceRef(ref); })
              .catch(() => { if (active) setPriceRef(null); });
          }
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

  const gratis = listing.PriceRvm === 0;
  const isDonationMode = listing.Mode === "Donate" || listing.Mode === "Volunteer";
  const brl = !gratis ? brlEstimate(listing.PriceRvm, rate ?? 0) : null;
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
                alt={listing.Title}
                className="w-full h-full object-cover"
              />
            ) : (
              <span aria-hidden>{listing.Kind === "Service" ? "🛠️" : "📦"}</span>
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
            <Badge modo={listing.Mode} size="md" />
            <span className="text-sm text-silver">{KIND_LABELS[listing.Kind]}</span>
            {listing.NftTokenId && (
              <span className="text-xs text-silver/50">
                NFT #{listing.NftTokenId}
              </span>
            )}
          </div>

          <h1 className="text-3xl font-bold text-cream">{listing.Title}</h1>
          <div className="mt-3">
            <div className="text-2xl">
              {gratis ? (
                <span className="rms text-lima">Grátis</span>
              ) : (
                <span className="rms text-cream">
                  RM$ {listing.PriceRvm.toLocaleString("pt-BR")}
                </span>
              )}
            </div>
            {gratis ? (
              isDonationMode && (
                <p className="mt-1 text-sm text-silver/80">🎁 Doação</p>
              )
            ) : (
              brl && (
                <p className="mt-1 text-sm text-silver">
                  ≈ {brl}{" "}
                  <span className="text-silver/70">(estimativa simbólica)</span>
                </p>
              )
            )}
          </div>

          <p className="mt-4 text-cream/90 whitespace-pre-wrap">{listing.Description}</p>

          {!gratis && rate != null && (
            <p className="mt-3 text-xs text-silver/80 leading-relaxed border-l-2 border-smoke pl-3">
              {disclaimer ??
                "O valor em R$ é apenas uma estimativa simbólica para trocas — o RVM é crédito de troca da comunidade, não moeda."}
            </p>
          )}

          {/* Comparativo de preço justo (Pricing UF-28) */}
          {priceRef && !gratis && (
            <div className="mt-4 bg-charcoal/60 rounded-xl border border-smoke p-4">
              <h3 className="text-sm font-bold text-amber mb-2">
                Preço justo da categoria
              </h3>
              <div className="flex flex-wrap gap-4 text-sm">
                <div>
                  <span className="text-silver">Mediana: </span>
                  <span className="font-bold text-cream">RM$ {priceRef.RvmMedian.toLocaleString("pt-BR")}</span>
                </div>
                <div>
                  <span className="text-silver">Sugestão: </span>
                  <span className="font-bold text-esmeralda">RM$ {priceRef.FairSuggestionRvm.toLocaleString("pt-BR")}</span>
                </div>
              </div>
              <p className="text-xs text-silver/70 mt-2">
                {priceRef.SampleCount} anúncio(s) · {priceRef.SourcesUsed}
                {priceRef.LastIpcRate != null ? ` · IPCA ${priceRef.LastIpcRate}%` : ""}
              </p>
              {(() => {
                const diff = listing.PriceRvm - priceRef.RvmMedian;
                if (diff < -2) return <p className="text-xs text-lima mt-1">Abaixo da mediana — ótima oferta!</p>;
                if (diff > 2) return <p className="text-xs text-rosa mt-1">Acima da mediana.</p>;
                return <p className="text-xs text-sky mt-1">Na faixa da mediana.</p>;
              })()}
            </div>
          )}

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
            {(listing.City || listing.Neighborhood) && (
              <div className="flex gap-2">
                <dt className="text-silver w-28">Local:</dt>
                <dd className="text-cream">
                  {[listing.Neighborhood, listing.City].filter(Boolean).join(", ")}
                </dd>
              </div>
            )}
          </dl>

          {/* Vendedor — link para o perfil público */}
          <Link
            to={`/users/${listing.SellerId}`}
            className="mt-6 flex items-center gap-3 bg-smoke rounded-xl p-3 hover:border-esmeralda border border-transparent transition"
          >
            <Avatar
              name={listing.SellerName}
              src={listing.SellerAvatarUrl}
              size={40}
            />
            <div>
              <div className="text-cream font-medium">{listing.SellerName}</div>
              <div className="text-xs text-silver">
                {MODO_META[listing.Mode].emoji} {MODO_META[listing.Mode].label} · ver perfil →
              </div>
            </div>
          </Link>

          {/* Avaliações do vendedor (recebidas em trocas concluídas — UF-23) */}
          <SellerReviews vendedorId={listing.SellerId} />

          {/* CTA */}
          <div className="mt-6">
            <ListingActions listing={listing} user={user} />
          </div>

          {/* Salvar (bookmark pessoal) — qualquer usuário logado, inclusive o dono */}
          <SaveButton listingId={listing.Id} user={user} />

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

// Salvar/dessalvar anúncio (bookmark pessoal). Estado inicial via saved/ids
// (uma chamada, não uma por card). Login apenas — salvar não exige verificação.
function SaveButton({ listingId, user }: { listingId: string; user: AuthUser | null }) {
  const [saved, setSaved] = useState<boolean | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    if (!user) return;
    let active = true;
    api
      .savedListingIds()
      .then((ids) => {
        if (active) setSaved(ids.includes(listingId));
      })
      .catch(() => {
        if (active) setSaved(false);
      });
    return () => {
      active = false;
    };
  }, [user, listingId]);

  if (!user) return null;

  async function toggle() {
    setBusy(true);
    try {
      setSaved(await api.saveListing(listingId));
    } catch {
      /* mantém o estado atual — feedback opcional */
    } finally {
      setBusy(false);
    }
  }

  return (
    <button
      type="button"
      onClick={toggle}
      disabled={busy || saved === null}
      className={`mt-3 w-full border rounded-xl px-4 py-2.5 text-sm font-semibold transition disabled:opacity-60 ${
        saved
          ? "border-amber/60 text-amber bg-amber/10"
          : "border-smoke text-silver hover:text-cream hover:border-esmeralda"
      }`}
      aria-pressed={saved ?? false}
    >
      {saved ? "🔖 Salvo" : "🔖 Salvar anúncio"}
    </button>
  );
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
      .userReviews(vendedorId, 20)
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
          · média <Stars n={Math.round(avg)} />{" "}
          <span className="text-amber font-medium">
            {avg.toLocaleString("pt-BR", {
              minimumFractionDigits: 1,
              maximumFractionDigits: 1,
            })}
          </span>{" "}
          ({reviews.length})
        </span>
      </h3>
      <ul className="mt-3 space-y-3">
        {reviews.map((r) => (
          <li key={r.Id} className="text-sm">
            <div className="flex items-center gap-2 mb-0.5">
              <span className="text-cream font-medium">{r.ReviewerName}</span>
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
  const isOwner = !!user && user.userId === listing.SellerId;
  const isDonation = listing.Mode === "Donate" || listing.Mode === "Volunteer";
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
      setQueueOk(`Receptor escolhido: ${req.AuthorName}`);
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
                        name={req.AuthorName}
                        src={req.AuthorAvatarUrl}
                        size={28}
                      />
                      <span className="text-cream text-sm font-medium">
                        {req.AuthorName}
                      </span>
                      <span className="text-silver text-xs ml-auto">
                        {timeAgo(req.CreatedAt)}
                      </span>
                    </div>
                    <p className="text-cream/90 text-sm whitespace-pre-wrap">
                      {req.Message}
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
                          : `Selecionar ${req.AuthorName.split(" ")[0]}`}
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
