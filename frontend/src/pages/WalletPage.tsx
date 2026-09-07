import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Gift, Wallet } from "lucide-react";
import { ApiError, api } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import type { WalletBalance } from "../api/types";

// Carteira do usuário logado (GET /api/wallet/balance — leitura on-chain que
// degrada em silêncio quando a chain está offline) + resgate de cupom (UF-29):
// o RVM mintado cai direto aqui, o campo de cupom mora na carteira.
export function WalletPage() {
  const { user } = useAuth();
  const [data, setData] = useState<WalletBalance | null>(null);
  const [erro, setErro] = useState(false);
  const [reload, setReload] = useState(0);

  useEffect(() => {
    if (!user) return;
    let active = true;
    setErro(false);
    api
      .walletBalance()
      .then((b) => {
        if (active) setData(b);
      })
      .catch(() => {
        if (active) setErro(true);
      });
    return () => {
      active = false;
    };
  }, [user, reload]);

  if (!user) {
    return (
      <div className="app-container">
        <div className="app-read">
          <h1 className="text-2xl font-bold text-cream mb-6">Carteira RVM</h1>
          <div className="bg-charcoal rounded-2xl border border-smoke p-8 text-center">
            <div className="mb-3 flex justify-center text-silver" aria-hidden>
              <Wallet className="w-10 h-10" />
            </div>
            <p className="text-silver text-sm mb-6">
              Entre na sua conta para ver seu saldo de créditos de troca.
            </p>
            <Link to="/login" className="bg-brand text-ink font-semibold px-5 py-2.5 rounded-xl">
              Entrar
            </Link>
          </div>
        </div>
      </div>
    );
  }

  const saldo = typeof data?.Rvm === "number";

  return (
    <div className="app-container">
      <div className="app-read">
        <h1 className="text-2xl font-bold text-cream mb-6">Carteira RVM</h1>
        <div className="bg-charcoal rounded-2xl border border-smoke p-8 text-center">
          <div className="mb-3 flex justify-center text-silver" aria-hidden>
            <Wallet className="w-8 h-8" />
          </div>
          {erro ? (
            <p className="text-silver text-sm mb-6">
              Não foi possível ler sua carteira agora. Tente novamente mais tarde.
            </p>
          ) : (
            <>
              <p className="rms text-3xl text-cream mb-1">
                RM${" "}
                {saldo
                  ? data!.Rvm!.toLocaleString("pt-BR", { maximumFractionDigits: 2 })
                  : "—"}
              </p>
              {data?.WalletAddress ? (
                <p className="text-xs text-silver/70 mb-2 break-all">
                  Carteira: {data.WalletAddress}
                </p>
              ) : null}
              <p className="text-silver text-sm mb-6">
                {saldo
                  ? "Créditos de troca disponíveis (RM$ = crédito, não cripto)."
                  : "Saldo indisponível agora (rede offline) — seus créditos continuam salvos."}
              </p>
            </>
          )}
          <p className="text-xs text-silver/60 mb-6">
            Self-custody real (carteira invisível via passkey) chega com a Account
            Abstraction.
          </p>
          <Link to="/feed" className="bg-brand text-ink font-semibold px-5 py-2.5 rounded-xl">
            Ver o feed
          </Link>
        </div>

        {user.verified && (
          <RedeemCoupon onRedeemed={() => setReload((n) => n + 1)} />
        )}
      </div>
    </div>
  );
}

// Resgatar cupom (UF-29) — exige conta verificada (gate Verified no backend).
// Após o resgate o saldo acima é relido (o mint cai na mesma carteira).
function RedeemCoupon({ onRedeemed }: { onRedeemed: () => void }) {
  const [code, setCode] = useState("");
  const [busy, setBusy] = useState(false);
  const [msg, setMsg] = useState<string | null>(null);
  const [err, setErr] = useState<string | null>(null);

  async function submit(ev: React.FormEvent) {
    ev.preventDefault();
    const trimmed = code.trim();
    if (!trimmed) return;
    setBusy(true);
    setMsg(null);
    setErr(null);
    try {
      await api.redeemCoupon(trimmed);
      setMsg("Cupom resgatado! O RVM foi creditado na sua carteira.");
      setCode("");
      onRedeemed();
    } catch (e) {
      setErr(e instanceof ApiError ? e.message : "Falha ao resgatar cupom.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <form
      onSubmit={submit}
      className="bg-charcoal rounded-2xl border border-smoke p-4 mt-6"
    >
      <h2 className="flex items-center gap-1.5 text-cream font-semibold mb-2">
        <Gift aria-hidden className="w-4 h-4" />
        Resgatar cupom
      </h2>
      <p className="text-xs text-silver mb-3">
        Tem um código de cupom? O RVM é creditado direto na sua carteira.
      </p>
      <div className="flex gap-2">
        <input
          type="text"
          value={code}
          onChange={(e) => setCode(e.target.value.toUpperCase())}
          placeholder="CÓDIGO DO CUPOM"
          className="flex-1 min-w-0 bg-ink text-cream rounded-lg border border-smoke focus:border-esmeralda px-3 py-2 outline-none text-sm uppercase tracking-wider"
        />
        <button
          type="submit"
          disabled={busy || !code.trim()}
          className="bg-brand text-ink font-semibold px-5 py-2 rounded-xl text-sm disabled:opacity-60"
        >
          {busy ? "…" : "Resgatar"}
        </button>
      </div>
      {msg && <p className="text-esmeralda text-sm mt-2">{msg}</p>}
      {err && <p className="text-rosa text-sm mt-2">{err}</p>}
    </form>
  );
}
