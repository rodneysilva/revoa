import { Link } from "react-router-dom";
import { BadgeCheck, Wallet } from "lucide-react";
import { useAuth } from "../auth/AuthContext";
import { Avatar } from "../components/Avatar";
import {
  ProfileActivity,
  ProfileCommunities,
  ProfileListings,
  ProfileReputation,
  ProfileSaved,
} from "../components/ProfileActivity";
import type { ReactNode } from "react";

export function ProfilePage() {
  const { user, logout } = useAuth();

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

  return (
    <div className="app-container">
      <header className="flex items-center gap-4 mb-6">
        <Avatar name={user.nome || "?"} size={56} />
        <div>
          <h1 className="flex flex-wrap items-center gap-2 text-2xl font-bold text-cream">
            {user.nome || "Perfil"}
            {user.verified && (
              <span className="inline-flex items-center gap-1 text-esmeralda text-sm font-semibold">
                <BadgeCheck aria-hidden className="w-4 h-4" /> Verificado
              </span>
            )}
          </h1>
          <p className="text-sm text-silver">{user.email || "—"}</p>
        </div>
      </header>

      {/* Full width com distribuição em duas colunas (padrão das outras páginas):
          sidebar com dados/cupom/ações + coluna principal com a atividade pública. */}
      <div className="grid gap-6 lg:grid-cols-[20rem_minmax(0,1fr)] items-start">
        <aside className="space-y-6">
          <div className="bg-charcoal rounded-2xl border border-smoke p-6 space-y-3">
            <Row
              label="Status"
              value={
                user.verified ? (
                  <span className="inline-flex items-center gap-1 text-esmeralda font-semibold"><BadgeCheck aria-hidden className="w-4 h-4" /> Verificado</span>
                ) : (
                  <span className="text-amber font-semibold">Pendente de verificação</span>
                )
              }
            />
            <Row
              label="Carteira"
              value={
                <Link
                  to="/wallet"
                  className="inline-flex items-center gap-1 text-lima hover:text-cream"
                >
                  <Wallet aria-hidden className="w-4 h-4" />
                  Ver saldo
                </Link>
              }
            />
          </div>

          <div className="flex flex-col gap-3">
            <Link
              to="/trades"
              className="bg-brand text-ink font-semibold px-5 py-2.5 rounded-xl text-center"
            >
              Minhas trocas
            </Link>
            <button
              onClick={logout}
              className="border border-smoke text-silver hover:text-rosa px-5 py-2.5 rounded-xl"
            >
              Sair
            </button>
          </div>
        </aside>

        <div className="min-w-0">
          {/* Atividade pública — o que o resto da Revoa vê em /users/{id}. */}
          <ProfileReputation userId={user.userId} />
          <ProfileListings userId={user.userId} self />
          <ProfileSaved />
          <ProfileCommunities userId={user.userId} />
          <ProfileActivity userId={user.userId} />
        </div>
      </div>

      <p className="mt-6 text-xs text-silver">
        Edição de perfil e avatar em breve.
      </p>
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
