import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Check, Gift, Star } from "lucide-react";
import { ApiError, api } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import { Avatar } from "../components/Avatar";
import { Badge } from "../components/Badge";
import { KIND_LABELS } from "../lib/config";
import { timeAgo } from "../lib/time";
import type { Trade, TradeState } from "../api/types";

const HAPPY: TradeState[] = ["Offered", "Funded", "Released"];
const BRANCH: TradeState[] = ["Disputed", "Refunded", "Cancelled"];

const STATE_STYLE: Record<TradeState, string> = {
  Offered: "text-silver border-smoke",
  Funded: "text-sky border-sky/40",
  Released: "text-esmeralda border-esmeralda/40",
  Disputed: "text-amber border-amber/40",
  Refunded: "text-sky border-sky/40",
  Cancelled: "text-rosa border-rosa/40",
};

const BRANCH_STYLE: Record<TradeState, string> = {
  Offered: "",
  Funded: "",
  Released: "",
  Disputed: "text-amber",
  Refunded: "text-sky",
  Cancelled: "text-rosa",
};

const BTN_PRI =
  "bg-brand text-ink text-xs font-semibold px-3 py-2 rounded-lg disabled:opacity-60";
const BTN_SEC =
  "bg-smoke text-cream text-xs font-semibold px-3 py-2 rounded-lg disabled:opacity-60 border border-smoke";
const BTN_WARN =
  "bg-rosa/15 text-rosa text-xs font-semibold px-3 py-2 rounded-lg disabled:opacity-60 border border-rosa/40";

function happyIndex(state: TradeState, funded: boolean): number {
  if (state === "Released") return 2;
  if (state === "Funded") return 1;
  if (state === "Disputed" || state === "Refunded") return 1;
  if (state === "Cancelled") return funded ? 1 : 0;
  return 0;
}

function tradeTimestamp(t: Trade): number {
  const candidates = [t.ReleasedAt, t.FundedAt].filter(Boolean) as string[];
  if (!candidates.length) return 0;
  return Math.max(...candidates.map((d) => new Date(d).getTime()));
}

async function fetchTrades(userId: string): Promise<Trade[]> {
  const [asBuyer, asSeller] = await Promise.all([
    api.tradeHistory({ buyerId: userId }).catch(() => [] as Trade[]),
    api.tradeHistory({ sellerId: userId }).catch(() => [] as Trade[]),
  ]);
  const map = new Map<string, Trade>();
  [...asBuyer, ...asSeller].forEach((t) => map.set(t.Id, t));
  return Array.from(map.values()).sort(
    (a, b) => tradeTimestamp(b) - tradeTimestamp(a)
  );
}

function Stepper({ trade }: { trade: Trade }) {
  const idx = happyIndex(trade.State, !!trade.FundedAt);
  const branch = BRANCH.includes(trade.State) ? trade.State : null;
  return (
    <div className="space-y-2">
      <ol className="flex items-center gap-1 text-xs">
        {HAPPY.map((s, i) => {
          const done = i <= idx;
          return (
            <li key={s} className="flex items-center gap-1">
              <span
                className={`inline-flex items-center justify-center w-5 h-5 rounded-full border text-[10px] ${
                  done
                    ? "bg-esmeralda text-ink border-transparent"
                    : "bg-charcoal text-silver border-smoke"
                }`}
              >
                {done ? <Check aria-hidden className="w-3 h-3" /> : i + 1}
              </span>
              <span className={done ? "text-cream" : "text-silver"}>{s}</span>
              {i < HAPPY.length - 1 && (
                <span className="text-smoke mx-0.5">›</span>
              )}
            </li>
          );
        })}
      </ol>
      {branch && (
        <div className={`text-xs font-semibold ${BRANCH_STYLE[branch]}`}>
          ↳ {branch}
          {branch === "Disputed" && " · aguardando resolução"}
        </div>
      )}
    </div>
  );
}

// Box de avaliação pós-troca (UF-23). Mostra só p/ trade Liberada onde o usuário é parte.
// Simplificação (spec): tenta criar; se voltar "já avaliou", marca como concluído.
function ReviewBox({ trade, counterNome }: { trade: Trade; counterNome: string }) {
  const [rating, setRating] = useState(0);
  const [hover, setHover] = useState(0);
  const [comment, setComment] = useState("");
  const [busy, setBusy] = useState(false);
  const [done, setDone] = useState(false);
  const [already, setAlready] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  async function submit() {
    if (rating < 1 || rating > 5) {
      setErr("Selecione de 1 a 5 estrelas.");
      return;
    }
    setBusy(true);
    setErr(null);
    try {
      await api.createReview(trade.Id, rating, comment.trim() || undefined);
      setDone(true);
    } catch (e) {
      const msg = e instanceof ApiError ? e.message : "falha ao enviar.";
      if (/já avaliou/i.test(msg)) {
        setAlready(true);
      } else {
        setErr(msg);
      }
    } finally {
      setBusy(false);
    }
  }

  if (done) {
    return (
      <p className="mt-3 text-xs font-semibold text-esmeralda">
        <span className="inline-flex items-center gap-1"><Star aria-hidden className="w-3.5 h-3.5 fill-current" /> Avaliação enviada — obrigado!</span>
      </p>
    );
  }

  if (already) {
    return (
      <p className="mt-3 inline-flex items-center gap-1 text-xs font-semibold text-silver">
        <Check aria-hidden className="w-3.5 h-3.5" /> Avaliado
      </p>
    );
  }

  const active = hover || rating;

  return (
    <div className="mt-3 border border-smoke rounded-lg p-3 bg-charcoal">
      <div className="text-xs text-cream mb-2">
        Avaliar {counterNome || "a contraparte"}
      </div>
      <div className="flex gap-1 mb-2" role="group" aria-label="Nota em estrelas">
        {[1, 2, 3, 4, 5].map((n) => (
          <button
            key={n}
            type="button"
            disabled={busy}
            onClick={() => setRating(n)}
            onMouseEnter={() => setHover(n)}
            onMouseLeave={() => setHover(0)}
            aria-label={`${n} estrela${n > 1 ? "s" : ""}`}
            className={`transition-colors ${
              n <= active ? "text-amber" : "text-smoke hover:text-silver"
            }`}
          >
            <Star aria-hidden className={`w-6 h-6 ${n <= active ? "fill-current" : ""}`} />
          </button>
        ))}
      </div>
      <textarea
        value={comment}
        onChange={(e) => setComment(e.target.value)}
        disabled={busy}
        rows={2}
        placeholder="Comentário (opcional)"
        className="w-full bg-ink text-cream text-xs rounded-md border border-smoke px-2 py-1.5 mb-2 resize-none focus:outline-none focus:border-brand"
      />
      {err && <p className="text-rosa text-xs mb-2">{err}</p>}
      <button
        type="button"
        disabled={busy}
        onClick={submit}
        className="bg-brand text-ink text-xs font-semibold px-3 py-2 rounded-lg disabled:opacity-60"
      >
        {busy ? "Enviando…" : "Enviar avaliação"}
      </button>
    </div>
  );
}

function TradeCard({
  trade,
  userId,
  onReload,
}: {
  trade: Trade;
  userId: string;
  onReload: () => void;
}) {
  const isSeller = trade.SellerId === userId;
  const counterNome = isSeller ? trade.BuyerName : trade.SellerName;
  const counterAvatar = isSeller ? trade.BuyerAvatarUrl : trade.SellerAvatarUrl;
  const [busy, setBusy] = useState<string | null>(null);
  const [err, setErr] = useState<string | null>(null);
  const [releaseToSeller, setReleaseToSeller] = useState(true);

  async function run(
    action: string,
    label: string,
    fn: () => Promise<unknown>,
    confirmMsg?: string
  ) {
    if (confirmMsg && !window.confirm(confirmMsg)) return;
    setBusy(action);
    setErr(null);
    try {
      await fn();
      onReload();
    } catch (e) {
      setErr(`${label}: ${e instanceof ApiError ? e.message : "falha."}`);
    } finally {
      setBusy(null);
    }
  }

  return (
    <div className="bg-charcoal rounded-xl border border-smoke p-4 flex flex-col">
      <div className="flex items-center gap-2 mb-2 flex-wrap">
        <Badge modo={trade.Mode} />
        <span className="text-xs text-silver">{KIND_LABELS[trade.Kind]}</span>
        <span
          className={`ml-auto text-xs font-semibold px-2 py-0.5 rounded-full border ${STATE_STYLE[trade.State]}`}
        >
          {trade.State}
        </span>
      </div>

      <div className="text-lg mb-2">
        {trade.IsDonation ? (
          <span className="inline-flex items-center gap-1 text-lima"><Gift aria-hidden className="w-4 h-4" /> Doação</span>
        ) : (
          <span className="rms text-cream">
            RM$ {trade.TotalRvm.toLocaleString("pt-BR")}
          </span>
        )}
      </div>

      <div className="flex items-center gap-2 mb-3">
        <Avatar name={counterNome} src={counterAvatar} size={28} />
        <span className="text-xs text-silver">
          {isSeller
            ? `Você vende p/ ${counterNome || "—"}`
            : `Você comprou de ${counterNome || "—"}`}
        </span>
      </div>

      <div className="pt-3 border-t border-smoke">
        <Stepper trade={trade} />
      </div>

      {(trade.FundedAt || trade.ReleasedAt) && (
        <div className="mt-2 flex flex-wrap gap-x-3 gap-y-0.5 text-[11px] text-silver/70">
          {trade.FundedAt && <span>Financiada {timeAgo(trade.FundedAt)}</span>}
          {trade.ReleasedAt && <span>Liberada {timeAgo(trade.ReleasedAt)}</span>}
        </div>
      )}

      {trade.State === "Released" && (
        <ReviewBox trade={trade} counterNome={counterNome} />
      )}

      {err && <p className="text-rosa text-xs mt-2">{err}</p>}

      {trade.State === "Funded" && (
        <div className="mt-3 flex flex-wrap gap-2">
          {trade.Kind === "Service" && !isSeller && !trade.VoucherRedeemed && (
            <button
              type="button"
              disabled={!!busy}
              onClick={() =>
                run("redeem", "Confirmar serviço", () => api.redeem(trade.Id))
              }
              className={BTN_PRI}
            >
              {busy === "redeem" ? "…" : "Confirmar serviço"}
            </button>
          )}
          <button
            type="button"
            disabled={!!busy}
            onClick={() =>
              run("release", "Liberar", () => api.release(trade.Id))
            }
            className={BTN_PRI}
          >
            {busy === "release" ? "…" : "Liberar"}
          </button>
          <button
            type="button"
            disabled={!!busy}
            onClick={() =>
              run(
                "dispute",
                "Abrir disputa",
                () => api.dispute(trade.Id),
                "Abrir disputa desta troca?"
              )
            }
            className={BTN_WARN}
          >
            {busy === "dispute" ? "…" : "Abrir disputa"}
          </button>
          <button
            type="button"
            disabled={!!busy}
            onClick={() =>
              run(
                "cancel",
                "Cancelar",
                () => api.cancelTrade(trade.Id),
                "Cancelar esta troca de comum acordo?"
              )
            }
            className={BTN_SEC}
          >
            {busy === "cancel" ? "…" : "Cancelar"}
          </button>
        </div>
      )}

      {trade.State === "Disputed" && (
        <div className="mt-3">
          <p className="text-xs text-silver mb-2">
            Aguardando resolução da equipe.
          </p>
          {import.meta.env.DEV && (
            <div className="border border-dashed border-smoke rounded-lg p-2">
              <div className="text-[10px] uppercase tracking-wide text-amber mb-1">
                DEV · modo árbitro
              </div>
              <label className="flex items-center gap-2 text-xs text-cream mb-2">
                <input
                  type="checkbox"
                  checked={releaseToSeller}
                  onChange={(e) => setReleaseToSeller(e.target.checked)}
                />
                Liberar para o vendedor
              </label>
              <button
                type="button"
                disabled={!!busy}
                onClick={() =>
                  run(
                    "resolve",
                    "Resolver",
                    () => api.resolveTrade(trade.Id, releaseToSeller),
                    `Resolver a favor do ${
                      releaseToSeller ? "vendedor" : "comprador"
                    }?`
                  )
                }
                className={BTN_WARN}
              >
                {busy === "resolve"
                  ? "…"
                  : "Resolver como árbitro (dev)"}
              </button>
            </div>
          )}
        </div>
      )}

      <Link
        to={`/listings/${trade.ListingId}`}
        className="mt-3 text-xs text-esmeralda hover:underline"
      >
        ver anúncio →
      </Link>
    </div>
  );
}

export function TradeTrackerPage() {
  const { user } = useAuth();
  const [trades, setTrades] = useState<Trade[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = () => {
    if (!user) return;
    setLoading(true);
    setError(null);
    fetchTrades(user.userId)
      .then(setTrades)
      .catch((e) => {
        setError(
          e instanceof ApiError ? e.message : "Erro ao carregar trocas."
        );
      })
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    let active = true;
    if (!user) return;
    setLoading(true);
    fetchTrades(user.userId)
      .then((t) => {
        if (active) setTrades(t);
      })
      .catch((e) => {
        if (active)
          setError(e instanceof ApiError ? e.message : "Erro ao carregar trocas.");
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, [user]);

  if (!user)
    return (
      <div className="app-container">
        <div className="app-read text-center">
          <h1 className="text-2xl font-bold text-cream mb-2">Suas trocas</h1>
          <p className="text-silver mb-4">
            Entre para acompanhar suas trocas e doações.
          </p>
          <Link
            to="/login"
            className="bg-brand text-ink font-semibold px-6 py-3 rounded-xl"
          >
            Entrar
          </Link>
        </div>
      </div>
    );

  return (
    <div className="app-container">
      <h1 className="text-2xl font-bold text-cream mb-6">Suas trocas</h1>
      {error && (
        <div className="bg-smoke border border-smoke text-silver rounded-xl p-4 text-sm mb-6">
          {error}
        </div>
      )}
      {loading ? (
        <p className="text-silver">Carregando…</p>
      ) : trades.length === 0 ? (
        <div className="bg-charcoal rounded-xl border border-smoke p-8 text-center">
          <p className="text-silver mb-4">
            Você ainda não fez trocas. Explore o feed!
          </p>
          <Link
            to="/feed"
            className="bg-brand text-ink font-semibold px-5 py-2.5 rounded-xl"
          >
            Explorar o feed
          </Link>
        </div>
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-3 2xl:grid-cols-4">
          {trades.map((t) => (
            <TradeCard
              key={t.Id}
              trade={t}
              userId={user.userId}
              onReload={load}
            />
          ))}
        </div>
      )}
    </div>
  );
}
