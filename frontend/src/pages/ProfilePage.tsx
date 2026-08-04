import { Link } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import type { ReactNode } from "react";

export function ProfilePage() {
  const { user, logout } = useAuth();

  if (!user)
    return (
      <div className="mx-auto max-w-md py-16 text-center">
        <h1 className="text-2xl font-bold text-cream mb-2">Perfil</h1>
        <p className="text-silver mb-4">Entre para ver seu perfil.</p>
        <Link to="/login" className="bg-brand text-ink font-semibold px-6 py-3 rounded-xl">
          Entrar
        </Link>
      </div>
    );

  return (
    <div className="mx-auto max-w-2xl py-8">
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
