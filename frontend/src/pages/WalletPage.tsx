import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { api } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import type { WalletBalance } from "../api/types";

// Carteira do usuário logado (GET /api/wallet/balance — leitura on-chain que
// degrada em silêncio quando a chain está offline).
export function WalletPage() {
  const { user } = useAuth();
  const [data, setData] = useState<WalletBalance | null>(null);
  const [erro, setErro] = useState(false);

  useEffect(() => {
    if (!user) return;
    let active = true;
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
  }, [user]);

  if (!user) {
    return (
      <div className="app-container">
        <div className="app-read">
          <h1 className="text-2xl font-bold text-cream mb-6">Carteira RVM</h1>
          <div className="bg-charcoal rounded-2xl border border-smoke p-8 text-center">
            <div className="text-5xl mb-3" aria-hidden>
              👛
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
          <div className="text-5xl mb-3" aria-hidden>
            👛
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
      </div>
    </div>
  );
}
