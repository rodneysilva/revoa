import { useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import { ApiError, api } from "../api/client";
import type { ReactNode } from "react";

export function ProfilePage() {
  const { user, logout } = useAuth();
  const [couponCode, setCouponCode] = useState("");
  const [couponBusy, setCouponBusy] = useState(false);
  const [couponMsg, setCouponMsg] = useState<string | null>(null);
  const [couponErr, setCouponErr] = useState<string | null>(null);

  if (!user)
    return (
      <div className="app-container">
        <div className="app-read text-center">
        <h1 className="text-2xl font-bold text-cream mb-2">Perfil</h1>
        <p className="text-silver mb-4">Entre para ver seu perfil.</p>
        <Link to="/login" className="bg-brand text-ink font-semibold px-6 py-3 rounded-xl">
          Entrar
        </Link>
        </div>
      </div>
    );

  async function redeemCoupon(ev: React.FormEvent) {
    ev.preventDefault();
    const code = couponCode.trim();
    if (!code) return;
    setCouponBusy(true);
    setCouponMsg(null);
    setCouponErr(null);
    try {
      await api.redeemCoupon(code);
      setCouponMsg("Cupom resgatado! O RVM foi creditado na sua carteira.");
      setCouponCode("");
    } catch (e) {
      setCouponErr(e instanceof ApiError ? e.message : "Falha ao resgatar cupom.");
    } finally {
      setCouponBusy(false);
    }
  }

  return (
    <div className="app-container">
      <div className="app-read">
      <h1 className="text-2xl font-bold text-cream mb-6">Perfil</h1>
      <div className="bg-charcoal rounded-2xl border border-smoke p-6 space-y-3">
        <Row label="Nome" value={user.nome || "—"} />
        <Row label="E-mail" value={user.email || "—"} />
        <Row
          label="Status"
          value={
            user.verified ? (
              <span className="text-esmeralda font-semibold">✓ Verificado</span>
            ) : (
              <span className="text-amber font-semibold">Pendente de verificação</span>
            )
          }
        />
        <Row
          label="Saldo"
          value={<span className="rms text-cream">RM$ —</span>}
        />
      </div>

      {/* Resgatar cupom (UF-29) — exige conta verificada (gate Verified no backend). */}
      {user.verified && (
        <form
          onSubmit={redeemCoupon}
          className="bg-charcoal rounded-2xl border border-smoke p-4 mt-6"
        >
          <h2 className="text-cream font-semibold mb-2">Resgatar cupom</h2>
          <p className="text-xs text-silver mb-3">
            Tem um código de cupom? O RVM é creditado direto na sua carteira.
          </p>
          <div className="flex gap-2">
            <input
              type="text"
              value={couponCode}
              onChange={(e) => setCouponCode(e.target.value.toUpperCase())}
              placeholder="CÓDIGO DO CUPOM"
              className="flex-1 bg-ink text-cream rounded-lg border border-smoke focus:border-esmeralda px-3 py-2 outline-none text-sm uppercase tracking-wider"
            />
            <button
              type="submit"
              disabled={couponBusy || !couponCode.trim()}
              className="bg-brand text-ink font-semibold px-5 py-2 rounded-xl text-sm disabled:opacity-60"
            >
              {couponBusy ? "…" : "Resgatar"}
            </button>
          </div>
          {couponMsg && <p className="text-esmeralda text-sm mt-2">{couponMsg}</p>}
          {couponErr && <p className="text-rosa text-sm mt-2">{couponErr}</p>}
        </form>
      )}

      <div className="mt-6 flex gap-3">
        <Link to="/trades" className="bg-brand text-ink font-semibold px-5 py-2.5 rounded-xl">
          Minhas trocas
        </Link>
        <button
          onClick={logout}
          className="border border-smoke text-silver hover:text-rosa px-5 py-2.5 rounded-xl"
        >
          Sair
        </button>
      </div>
      <p className="mt-6 text-xs text-silver">
        Edição de perfil e avatar em breve.
      </p>
      </div>
    </div>
  );
}

function Row({ label, value }: { label: string; value: ReactNode }) {
  return (
    <div className="flex items-center justify-between gap-4 border-b border-smoke pb-2 last:border-0">
      <span className="text-sm text-silver">{label}</span>
      <span className="text-cream text-right truncate">{value}</span>
    </div>
  );
}
