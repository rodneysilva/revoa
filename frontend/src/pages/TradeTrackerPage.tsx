import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { ApiError, api } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import { MODO_META } from "../lib/config";
import type { Trade, TradeState } from "../api/types";

// Happy path: Ofertada → Financiada → Liberada.
const HAPPY: TradeState[] = ["Ofertada", "Financiada", "Liberada"];

const TERMINAL: Record<TradeState, { label: string; cls: string }> = {
  Ofertada: { label: "Ofertada", cls: "text-silver" },
  Financiada: { label: "Financiada", cls: "text-sky" },
  Liberada: { label: "Liberada", cls: "text-esmeralda" },
  Disputada: { label: "Disputada", cls: "text-amber" },
  Reembolsada: { label: "Reembolsada", cls: "text-sky" },
  Cancelada: { label: "Cancelada", cls: "text-rosa" },
};

function isTerminal(state: TradeState): boolean {
  return state === "Disputada" || state === "Reembolsada" || state === "Cancelada";
}

function Stepper({ state }: { state: TradeState }) {
  if (isTerminal(state)) {
    const t = TERMINAL[state];
    return (
      <div className={`inline-flex items-center gap-1 text-sm font-semibold ${t.cls}`}>
        ⚠ {t.label}
      </div>
    );
  }
  const currentIdx = HAPPY.indexOf(state);
  return (
    <ol className="flex items-center gap-1 text-xs">
      {HAPPY.map((s, i) => {
        const done = i <= currentIdx;
        const isCurrent = i === currentIdx;
        return (
          <li key={s} className="flex items-center gap-1">
            <span
              className={`inline-flex items-center justify-center w-6 h-6 rounded-full border ${
                done
                  ? "bg-esmeralda text-ink border-transparent"
                  : "bg-charcoal text-silver border-smoke"
              } ${isCurrent ? "ring-2 ring-esmeralda/50" : ""}`}
            >
              {done ? "✓" : i + 1}
            </span>
            <span className={done ? "text-cream" : "text-silver"}>{s}</span>
            {i < HAPPY.length - 1 && <span className="text-smoke mx-1">›</span>}
          </li>
        );
      })}
    </ol>
  );
}

function TradeCard({ trade, role }: { trade: Trade; role: "buyer" | "seller" }) {
  const counterparty = role === "buyer" ? trade.SellerNome : trade.BuyerNome;
  return (
    <div className="bg-charcoal rounded-xl border border-smoke p-4">
      <div className="flex items-center gap-2 mb-3">
        <span className="text-xs text-silver">{MODO_META[trade.Modo].emoji}</span>
        <span className="text-xs text-silver uppercase tracking-wide">
          {role === "buyer" ? "Você comprou" : "Você ofereceu"} · {trade.Kind}
        </span>
        {trade.IsDonation && (
          <span className="ml-auto text-xs bg-terracota/15 text-terracota px-2 py-0.5 rounded-full font-semibold">
            🎁 Doação
          </span>
        )}
      </div>
      <div className="flex items-center justify-between gap-2">
        <div>
          <div className="text-cream font-medium">
            {trade.TotalRvm === 0 ? (
              <span className="text-lima">Grátis</span>
            ) : (
              <span className="rms">RM$ {trade.TotalRvm.toLocaleString("pt-BR")}</span>
            )}
          </div>
          <div className="text-xs text-silver">
            {role === "buyer" ? "com" : "para"} {counterparty || "—"}
          </div>
        </div>
        <Link
          to={`/listings/${trade.ListingId}`}
          className="text-xs text-esmeralda hover:underline"
        >
          ver anúncio →
        </Link>
      </div>
      <div className="mt-3 pt-3 border-t border-smoke">
        <Stepper state={trade.State} />
      </div>
    </div>
  );
}

export function TradeTrackerPage() {
  const { user } = useAuth();
  const [trades, setTrades] = useState<Trade[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!user) return;
    let active = true;
    setLoading(true);
    Promise.all([
      api.trades({ buyerId: user.userId }).catch(() => [] as Trade[]),
      api.trades({ sellerId: user.userId }).catch(() => [] as Trade[]),
    ])
      .then(([asBuyer, asSeller]) => {
        if (!active) return;
        const map = new Map<string, Trade>();
        [...asBuyer, ...asSeller].forEach((t) => map.set(t.Id, t));
        setTrades(Array.from(map.values()));
      })
      .catch((e) => {
        if (active) setError(e instanceof ApiError ? e.message : "Erro ao carregar trocas.");
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
      <div className="mx-auto max-w-md py-16 text-center">
        <h1 className="text-2xl font-bold text-cream mb-2">Suas trocas</h1>
        <p className="text-silver mb-4">Entre para acompanhar suas trocas e doações.</p>
        <Link to="/login" className="bg-brand text-ink font-semibold px-6 py-3 rounded-xl">
          Entrar
        </Link>
      </div>
    );

  const buyerIds = new Set(
    trades.filter((t) => t.BuyerId === user.userId).map((t) => t.Id)
  );

  return (
    <div className="mx-auto max-w-4xl py-8">
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
          <p className="text-silver mb-4">Você ainda não participou de trocas.</p>
          <Link to="/feed" className="bg-brand text-ink font-semibold px-5 py-2.5 rounded-xl">
            Explorar o feed
          </Link>
        </div>
      ) : (
        <div className="grid gap-4 sm:grid-cols-2">
          {trades.map((t) => (
            <TradeCard key={t.Id} trade={t} role={buyerIds.has(t.Id) ? "buyer" : "seller"} />
          ))}
        </div>
      )}
    </div>
  );
}
