import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { ApiError, api } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import { isAdminUser } from "../lib/admin";
import type { AdminUser } from "../api/types";

const USER_STATUS_LABEL: Record<AdminUser["Status"], string> = {
  PendingVerification: "Pendente",
  Active: "Ativo",
  Banned: "Banido",
  Inactive: "Inativo",
};

// Painel admin de usuários (mesmo esqueleto do AdminReportsPage): lista todos
// os status, busca por nome/e-mail e ações ban/unban. O próprio admin não se banne.
export function AdminUsersPage() {
  const { user } = useAuth();
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [forbidden, setForbidden] = useState(false);
  const [query, setQuery] = useState("");
  const [busy, setBusy] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    setForbidden(false);
    try {
      setUsers(await api.adminUsers());
    } catch (e) {
      if (e instanceof ApiError && e.status === 403) {
        setForbidden(true);
      } else {
        setError(e instanceof ApiError ? e.message : "Falha ao carregar usuários.");
      }
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (isAdminUser(user)) load();
    else {
      setLoading(false);
      setForbidden(true);
    }
  }, [load, user?.email]);

  async function setBanned(u: AdminUser, banned: boolean) {
    setBusy(u.Id);
    setError(null);
    try {
      if (banned) await api.adminBanUser(u.Id);
      else await api.adminUnbanUser(u.Id);
      await load();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : "Não foi possível atualizar o usuário.");
    } finally {
      setBusy(null);
    }
  }

  if (forbidden) {
    return (
      <div className="app-container text-center">
        <h1 className="text-2xl font-bold text-cream mb-3">Acesso restrito</h1>
        <p className="text-silver mb-4">
          Esta área é exclusiva de administradores.
        </p>
        <Link to="/feed" className="text-esmeralda hover:underline">
          ← Voltar ao feed
        </Link>
      </div>
    );
  }

  const q = query.trim().toLowerCase();
  const filtered = q
    ? users.filter(
        (u) =>
          u.Name.toLowerCase().includes(q) || u.Email.toLowerCase().includes(q)
      )
    : users;
  const bannedCount = users.filter((u) => u.Status === "Banned").length;

  return (
    <div className="app-container">
      <div className="flex items-center justify-between mb-4 flex-wrap gap-2">
        <h1 className="text-2xl font-bold text-cream">
          Usuários
          <span className="text-silver text-sm font-normal">
            {" "}
            · {users.length} no total
            {bannedCount > 0 && ` · ${bannedCount} banidos`}
          </span>
        </h1>
        <Link to="/admin" className="text-esmeralda text-sm hover:underline">
          ← Painel admin
        </Link>
      </div>

      <input
        type="search"
        value={query}
        onChange={(e) => setQuery(e.target.value)}
        placeholder="Buscar por nome ou e-mail…"
        className="w-full bg-charcoal text-cream rounded-xl border border-smoke focus:border-esmeralda px-4 py-2.5 outline-none text-sm mb-4"
      />

      {error && <p className="text-rosa text-sm mb-4">{error}</p>}

      {loading ? (
        <p className="text-silver">Carregando…</p>
      ) : filtered.length === 0 ? (
        <p className="text-silver">Nenhum usuário encontrado.</p>
      ) : (
        <ul className="space-y-2">
          {filtered.map((u) => {
            const isSelf = u.Id === user?.userId;
            const isBanned = u.Status === "Banned";
            return (
              <li
                key={u.Id}
                className="bg-smoke rounded-xl border border-smoke p-3 flex items-center gap-3 flex-wrap"
              >
                <div className="min-w-0 flex-1">
                  <p className="text-cream font-semibold truncate">
                    {u.Name}
                    {isSelf && (
                      <span className="text-xs text-esmeralda ml-2">(você)</span>
                    )}
                  </p>
                  <p className="text-silver text-sm truncate">{u.Email}</p>
                </div>

                <span
                  className={`text-xs rounded-full px-2 py-0.5 ${
                    isBanned
                      ? "bg-rosa/20 text-rosa"
                      : u.Status === "Active"
                        ? "bg-esmeralda/20 text-esmeralda"
                        : "bg-charcoal text-silver"
                  }`}
                >
                  {USER_STATUS_LABEL[u.Status]}
                </span>

                {(u.Status === "Active" || u.Status === "Banned") && !isSelf && (
                  <button
                    type="button"
                    onClick={() => setBanned(u, !isBanned)}
                    disabled={busy !== null}
                    className={`text-sm px-3 py-1.5 rounded-lg disabled:opacity-60 ${
                      isBanned
                        ? "bg-charcoal text-silver hover:text-cream"
                        : "bg-rosa text-ink font-semibold"
                    }`}
                  >
                    {busy === u.Id ? "…" : isBanned ? "Reabilitar" : "Banir"}
                  </button>
                )}
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}
