import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { ApiError, api } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import { isAdminEmail } from "../lib/admin";
import type { Coupon } from "../api/types";

const COUPON_PAGE_SIZE = 50;

function formatDate(iso?: string): string {
  if (!iso) return "—";
  try {
    return new Date(iso).toLocaleDateString("pt-BR", {
      day: "2-digit",
      month: "2-digit",
      year: "numeric",
    });
  } catch {
    return iso;
  }
}

export function AdminCouponsPage() {
  const { user } = useAuth();
  const [coupons, setCoupons] = useState<Coupon[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [forbidden, setForbidden] = useState(false);
  const [busy, setBusy] = useState(false);
  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(false);
  const [loadingMore, setLoadingMore] = useState(false);

  // Form de criação.
  const [amount, setAmount] = useState("");
  const [maxUses, setMaxUses] = useState("100");
  const [expiry, setExpiry] = useState("");
  const [code, setCode] = useState("");
  const [created, setCreated] = useState<Coupon | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    setForbidden(false);
    try {
      const data = await api.coupons(1);
      setCoupons(data);
      setPage(1);
      setHasMore(data.length >= COUPON_PAGE_SIZE);
    } catch (e) {
      if (e instanceof ApiError && e.status === 403) {
        setForbidden(true);
      } else {
        setError(e instanceof ApiError ? e.message : "Falha ao carregar cupons.");
      }
    } finally {
      setLoading(false);
    }
  }, []);

  async function loadMore() {
    if (loadingMore || !hasMore) return;
    setLoadingMore(true);
    const next = page + 1;
    try {
      const data = await api.coupons(next);
      setCoupons((prev) => [...prev, ...data]);
      setPage(next);
      setHasMore(data.length >= COUPON_PAGE_SIZE);
    } catch (e) {
      setError(e instanceof ApiError ? e.message : "Falha ao carregar mais cupons.");
    } finally {
      setLoadingMore(false);
    }
  }

  useEffect(() => {
    if (isAdminEmail(user?.email)) load();
    else {
      setLoading(false);
      setForbidden(true);
    }
  }, [load, user?.email]);

  async function create(ev: React.FormEvent) {
    ev.preventDefault();
    const amountRvm = Number(amount);
    const maxUsesNum = Number(maxUses);
    if (!Number.isFinite(amountRvm) || amountRvm <= 0) {
      setError("Informe uma quantidade de RVM válida.");
      return;
    }
    if (!Number.isFinite(maxUsesNum) || maxUsesNum < 0) {
      setError("Informe um número máximo de usos válido.");
      return;
    }

    setBusy(true);
    setError(null);
    setCreated(null);
    try {
      const coupon = await api.createCoupon({
        AmountRvm: amountRvm,
        MaxUses: maxUsesNum,
        Expiry: expiry ? new Date(expiry).toISOString() : undefined,
        Code: code.trim() || undefined,
      });
      setCreated(coupon);
      setAmount("");
      setMaxUses("100");
      setExpiry("");
      setCode("");
      await load();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : "Não foi possível criar o cupom.");
    } finally {
      setBusy(false);
    }
  }

  async function revoke(id: string) {
    if (!confirm("Revogar este cupom? Ele não poderá mais ser resgatado.")) return;
    setBusy(true);
    setError(null);
    try {
      await api.revokeCoupon(id);
      await load();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : "Não foi possível revogar o cupom.");
    } finally {
      setBusy(false);
    }
  }

  if (forbidden) {
    return (
      <div className="app-container text-center">
        <h1 className="text-2xl font-bold text-cream mb-3">Acesso restrito</h1>
        <p className="text-silver mb-4">Esta área é exclusiva de administradores.</p>
        <Link to="/feed" className="text-esmeralda hover:underline">
          ← Voltar ao feed
        </Link>
      </div>
    );
  }

  return (
    <div className="app-container">
      <h1 className="text-2xl font-bold text-cream mb-4">Cupons</h1>

      {/* Criar cupom */}
      <form
        onSubmit={create}
        className="bg-charcoal rounded-2xl border border-smoke p-4 mb-6 space-y-3"
      >
        <h2 className="text-cream font-semibold">Criar cupom</h2>
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <label className="block">
            <span className="text-xs text-silver">RVM por resgate</span>
            <input
              type="number"
              min="1"
              value={amount}
              onChange={(e) => setAmount(e.target.value)}
              placeholder="ex.: 5"
              required
              className="mt-1 w-full bg-ink text-cream rounded-lg border border-smoke focus:border-esmeralda px-3 py-2 outline-none text-sm"
            />
          </label>
          <label className="block">
            <span className="text-xs text-silver">Máximo de usos (0 = ilimitado)</span>
            <input
              type="number"
              min="0"
              value={maxUses}
              onChange={(e) => setMaxUses(e.target.value)}
              className="mt-1 w-full bg-ink text-cream rounded-lg border border-smoke focus:border-esmeralda px-3 py-2 outline-none text-sm"
            />
          </label>
          <label className="block">
            <span className="text-xs text-silver">Expira em (opcional)</span>
            <input
              type="datetime-local"
              value={expiry}
              onChange={(e) => setExpiry(e.target.value)}
              className="mt-1 w-full bg-ink text-cream rounded-lg border border-smoke focus:border-esmeralda px-3 py-2 outline-none text-sm"
            />
          </label>
          <label className="block">
            <span className="text-xs text-silver">Código (opcional → gerado se vazio)</span>
            <input
              type="text"
              value={code}
              onChange={(e) => setCode(e.target.value.toUpperCase())}
              placeholder="auto"
              className="mt-1 w-full bg-ink text-cream rounded-lg border border-smoke focus:border-esmeralda px-3 py-2 outline-none text-sm uppercase"
            />
          </label>
        </div>
        <button
          type="submit"
          disabled={busy}
          className="bg-brand text-ink font-semibold px-5 py-2 rounded-xl text-sm disabled:opacity-60"
        >
          {busy ? "Criando…" : "Criar cupom"}
        </button>

        {created && (
          <div className="bg-esmeralda/10 border border-esmeralda/30 rounded-xl p-3">
            <p className="text-sm text-cream">
              Cupom criado. Compartilhe o código:
            </p>
            <p className="text-2xl font-bold tracking-widest text-esmeralda mt-1">
              {created.Code}
            </p>
            <p className="text-xs text-silver mt-1">
              {created.AmountRvm} RVM · {created.MaxUses === 0 ? "usos ilimitados" : `${created.MaxUses} usos`}
              {created.Expiry ? ` · expira ${formatDate(created.Expiry)}` : ""}
            </p>
          </div>
        )}
      </form>

      {error && <p className="text-rosa text-sm mb-4">{error}</p>}

      {/* Lista */}
      {loading ? (
        <p className="text-silver">Carregando…</p>
      ) : coupons.length === 0 ? (
        <p className="text-silver">Nenhum cupom criado ainda.</p>
      ) : (
        <ul className="space-y-3">
          {coupons.map((c) => {
            const isActive = c.Status === "Active";
            return (
              <li key={c.Id} className="bg-smoke rounded-xl border border-smoke p-4">
                <div className="flex items-center gap-2 flex-wrap mb-1">
                  <span className="text-lg font-bold tracking-widest text-cream">{c.Code}</span>
                  <span
                    className={`text-xs rounded-full px-2 py-0.5 ${
                      isActive ? "bg-esmeralda/20 text-esmeralda" : "bg-rosa/20 text-rosa"
                    }`}
                  >
                    {isActive ? "Ativo" : "Revogado"}
                  </span>
                  <span className="text-xs text-silver ml-auto">{formatDate(c.CreatedAt)}</span>
                </div>
                <p className="text-sm text-silver">
                  <span className="rms text-cream">{c.AmountRvm} RVM</span> ·{" "}
                  {c.MaxUses === 0 ? "usos ilimitados" : `máx. ${c.MaxUses} usos`}
                  {c.Expiry ? ` · expira ${formatDate(c.Expiry)}` : ""}
                </p>
                {isActive && (
                  <button
                    type="button"
                    onClick={() => revoke(c.Id)}
                    disabled={busy}
                    className="mt-2 text-sm px-3 py-1.5 rounded-lg bg-charcoal text-rosa hover:text-ink hover:bg-rosa disabled:opacity-60"
                  >
                    Revogar
                  </button>
                )}
              </li>
            );
          })}
        </ul>
      )}
      {!loading && hasMore && (
        <div className="text-center mt-6">
          <button
            type="button"
            onClick={loadMore}
            disabled={loadingMore}
            className="border border-smoke text-cream font-semibold px-6 py-2.5 rounded-xl hover:border-esmeralda disabled:opacity-60"
          >
            {loadingMore ? "Carregando…" : "Carregar mais"}
          </button>
        </div>
      )}
    </div>
  );
}
