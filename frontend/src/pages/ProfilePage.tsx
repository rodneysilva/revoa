import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { Heart, LogOut, Wallet } from "lucide-react";
import { api } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import { Avatar } from "../components/Avatar";
import {
  ProfileActivity,
  ProfileCommunities,
  ProfileListings,
  ProfileSaved,
  Stars,
} from "../components/ProfileActivity";
import type { Reputation, Review, WalletBalance } from "../api/types";

// Perfil próprio (/profile) em tela cheia: faixa de identidade (avatar + nome +
// e-mail + ações) com resumo em chips (reputação/avaliações/doações/saldo) e o
// conteúdo em ABAS full width — uma seção por vez, grades que escalam até o 4K.
// O selo "Verificado" é exclusivo do perfil público (/users/:id): aqui o dono
// já sabe o próprio estado.
const TABS = ["anuncios", "salvos", "comunidades", "atividade"] as const;
type Tab = (typeof TABS)[number];

const TAB_LABEL: Record<Tab, string> = {
  anuncios: "Anúncios",
  salvos: "Salvos",
  comunidades: "Comunidades",
  atividade: "Atividade",
};

export function ProfilePage() {
  const { user, logout } = useAuth();
  const [sp, setSp] = useSearchParams();
  const raw = sp.get("tab");
  const tab: Tab = TABS.includes(raw as Tab) ? (raw as Tab) : "anuncios";

  const [reputation, setReputation] = useState<Reputation | null>(null);
  const [reviews, setReviews] = useState<Review[]>([]);
  const [balance, setBalance] = useState<WalletBalance | null>(null);
  const [loaded, setLoaded] = useState(false);

  useEffect(() => {
    if (!user) return;
    let active = true;
    Promise.all([
      api.reputation(user.userId).catch(() => null),
      api.userReviews(user.userId).catch(() => [] as Review[]),
      api.walletBalance().catch(() => null),
    ]).then(([rep, revs, bal]) => {
      if (!active) return;
      setReputation(rep);
      setReviews(revs);
      setBalance(bal);
      setLoaded(true);
    });
    return () => {
      active = false;
    };
  }, [user]);

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

  // Resumo: média/count vêm da reputação quando existe; senão, das avaliações.
  const avg =
    reviews.length > 0
      ? reviews.reduce((s, r) => s + r.Rating, 0) / reviews.length
      : reputation?.AvgRating ?? 0;
  const count = reputation?.ReviewsCount ?? reviews.length;
  const hasSummary = loaded && (count > 0 || reviews.length > 0 || reputation != null);

  function setTab(t: Tab) {
    setSp(t === "anuncios" ? {} : { tab: t }, { replace: true });
  }

  return (
    <div className="app-container">
      {/* ══ Faixa de identidade — ações à direita, resumo em chips abaixo ══ */}
      <div className="flex flex-wrap items-center gap-x-4 gap-y-3">
        <Avatar name={user.nome || "?"} size={56} />
        <div className="min-w-0">
          <h1 className="text-2xl font-bold text-cream truncate">{user.nome || "Perfil"}</h1>
          <p className="text-sm text-silver truncate">{user.email || "—"}</p>

          {hasSummary && (
            <div className="mt-1.5 flex flex-wrap items-center gap-x-3 gap-y-1.5 text-sm text-silver">
              {count > 0 && (
                <span className="inline-flex items-center gap-1.5">
                  <Stars n={Math.round(avg)} />
                  <span className="text-cream font-semibold">
                    {avg.toLocaleString("pt-BR", { maximumFractionDigits: 1 })}
                  </span>
                  <span>· {count} avaliaç{count === 1 ? "ão" : "ões"}</span>
                </span>
              )}
              {reputation && reputation.DonationsCount > 0 && (
                <span className="inline-flex items-center gap-1">
                  <Heart aria-hidden className="w-3.5 h-3.5 text-terracota" />
                  {reputation.DonationsCount} doaç{reputation.DonationsCount === 1 ? "ão" : "ões"}
                </span>
              )}
              {typeof balance?.Rvm === "number" && (
                <Link
                  to="/wallet"
                  className="inline-flex items-center gap-1 text-lima hover:text-cream font-semibold"
                >
                  <Wallet aria-hidden className="w-3.5 h-3.5" />
                  RM$ {balance.Rvm.toLocaleString("pt-BR", { maximumFractionDigits: 2 })}
                </Link>
              )}
            </div>
          )}

          {reviews.length > 0 && (
            <div className="mt-2 flex flex-wrap gap-2">
              {reviews.slice(0, 3).map((r) => (
                <span
                  key={r.Id}
                  className="inline-flex items-center gap-2 bg-charcoal border border-smoke rounded-full py-0.5 pl-1 pr-3"
                  title={r.Comment || `${r.ReviewerName}: ${r.Rating} de 5`}
                >
                  <Avatar name={r.ReviewerName} size={20} />
                  <Stars n={r.Rating} />
                </span>
              ))}
            </div>
          )}
        </div>

        <div className="ml-auto flex items-center gap-2">
          <Link
            to="/trades"
            className="border border-smoke text-silver hover:text-cream px-4 py-2 rounded-xl text-sm font-semibold whitespace-nowrap"
          >
            Minhas trocas
          </Link>
          <button
            onClick={logout}
            className="inline-flex items-center gap-1.5 border border-smoke text-silver hover:text-rosa px-4 py-2 rounded-xl text-sm font-semibold whitespace-nowrap"
          >
            <LogOut aria-hidden className="w-4 h-4" />
            Sair
          </button>
        </div>
      </div>

      {/* ══ Abas full width — uma seção por vez ══ */}
      <nav className="mt-6 flex gap-5 overflow-x-auto border-b border-smoke" aria-label="Seções do perfil">
        {TABS.map((t) => (
          <button
            key={t}
            onClick={() => setTab(t)}
            aria-current={t === tab ? "page" : undefined}
            className={`shrink-0 pb-2.5 -mb-px border-b-2 text-sm font-semibold transition ${
              t === tab
                ? "border-esmeralda text-cream"
                : "border-transparent text-silver hover:text-cream"
            }`}
          >
            {TAB_LABEL[t]}
          </button>
        ))}
      </nav>

      <div className="mt-5">
        {tab === "anuncios" && <ProfileListings userId={user.userId} self embedded />}
        {tab === "salvos" && <ProfileSaved embedded />}
        {tab === "comunidades" && <ProfileCommunities userId={user.userId} embedded />}
        {tab === "atividade" && <ProfileActivity userId={user.userId} embedded />}
      </div>

      <p className="mt-8 text-xs text-silver">Edição de perfil e avatar em breve.</p>
    </div>
  );
}
