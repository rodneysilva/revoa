import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { ApiError, api } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import { isAdminUser } from "../lib/admin";
import type { AdminCommunity } from "../api/types";

// Painel admin de comunidades: TODAS (inclui arquivadas) com contagem de
// membros, busca por nome e ações arquivar/reativar. Arquivada some dos feeds.
export function AdminCommunitiesPage() {
  const { user } = useAuth();
  const [communities, setCommunities] = useState<AdminCommunity[]>([]);
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
      setCommunities(await api.adminCommunities());
    } catch (e) {
      if (e instanceof ApiError && e.status === 403) {
        setForbidden(true);
      } else {
        setError(e instanceof ApiError ? e.message : "Falha ao carregar comunidades.");
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

  async function setArchived(c: AdminCommunity, archived: boolean) {
    setBusy(c.Id);
    setError(null);
    try {
      if (archived) await api.adminArchiveCommunity(c.Id);
      else await api.adminReactivateCommunity(c.Id);
      await load();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : "Não foi possível atualizar a comunidade.");
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
    ? communities.filter((c) => c.Name.toLowerCase().includes(q))
    : communities;
  const archivedCount = communities.filter((c) => c.Status === "Archived").length;

  return (
    <div className="app-container">
      <div className="flex items-center justify-between mb-4 flex-wrap gap-2">
        <h1 className="text-2xl font-bold text-cream">
          Comunidades
          <span className="text-silver text-sm font-normal">
            {" "}
            · {communities.length} no total
            {archivedCount > 0 && ` · ${archivedCount} arquivadas`}
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
        placeholder="Buscar por nome…"
        className="w-full bg-charcoal text-cream rounded-xl border border-smoke focus:border-esmeralda px-4 py-2.5 outline-none text-sm mb-4"
      />

      {error && <p className="text-rosa text-sm mb-4">{error}</p>}

      {loading ? (
        <p className="text-silver">Carregando…</p>
      ) : filtered.length === 0 ? (
        <p className="text-silver">Nenhuma comunidade encontrada.</p>
      ) : (
        <ul className="space-y-2">
          {filtered.map((c) => {
            const isArchived = c.Status === "Archived";
            const local = [c.City, c.State].filter(Boolean).join(" - ");
            return (
              <li
                key={c.Id}
                className="bg-smoke rounded-xl border border-smoke p-3 flex items-center gap-3 flex-wrap"
              >
                <div className="min-w-0 flex-1">
                  <p className="text-cream font-semibold truncate">
                    {c.Name}
                    {local && (
                      <span className="text-silver text-xs font-normal ml-2">
                        {local}
                      </span>
                    )}
                  </p>
                  <p className="text-silver text-sm truncate">
                    {c.Type === "Default" ? "Comunidade da cidade" : "Criada por usuário"}
                    {" · "}
                    <span className="text-cream/70">{c.CreatorName}</span>
                    {" · "}
                    {c.MembersCount} membro{c.MembersCount === 1 ? "" : "s"}
                  </p>
                </div>

                <span
                  className={`text-xs rounded-full px-2 py-0.5 ${
                    isArchived
                      ? "bg-amber/20 text-amber"
                      : "bg-esmeralda/20 text-esmeralda"
                  }`}
                >
                  {isArchived ? "Arquivada" : "Ativa"}
                </span>

                <button
                  type="button"
                  onClick={() => setArchived(c, !isArchived)}
                  disabled={busy !== null}
                  className={`text-sm px-3 py-1.5 rounded-lg disabled:opacity-60 ${
                    isArchived
                      ? "bg-brand text-ink font-semibold"
                      : "bg-charcoal text-silver hover:text-cream"
                  }`}
                >
                  {busy === c.Id ? "…" : isArchived ? "Reativar" : "Arquivar"}
                </button>
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}
