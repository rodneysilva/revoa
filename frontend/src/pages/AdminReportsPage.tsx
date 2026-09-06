import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { ApiError, api } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import { isAdminUser } from "../lib/admin";
import type { Report, ReportStatus } from "../api/types";

const REPORT_PAGE_SIZE = 50;

const REASON_LABEL: Record<string, string> = {
  Spam: "Spam",
  Inappropriate: "Inadequado",
  Scam: "Golpe",
  Other: "Outro",
};

const TARGET_LABEL: Record<string, string> = {
  Listing: "Anúncio",
  Post: "Post",
  User: "Usuário",
  Comment: "Comentário",
};

const ACTION_LABEL: Record<string, string> = {
  Dismissed: "Arquivada",
  Warned: "Advertido",
  Banned: "Banido",
};

function timeAgo(iso: string): string {
  const diff = Date.now() - new Date(iso).getTime();
  if (!Number.isFinite(diff) || diff < 0) return "agora";
  const min = Math.floor(diff / 60000);
  if (min < 1) return "agora";
  if (min < 60) return `${min} min`;
  const h = Math.floor(min / 60);
  if (h < 24) return `${h} h`;
  const d = Math.floor(h / 24);
  return d === 1 ? "1 dia" : `${d} dias`;
}

export function AdminReportsPage() {
  const { user } = useAuth();
  const [reports, setReports] = useState<Report[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [forbidden, setForbidden] = useState(false);
  const [filter, setFilter] = useState<ReportStatus | "">("");
  const [notes, setNotes] = useState<Record<string, string>>({});
  const [busy, setBusy] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(false);
  const [loadingMore, setLoadingMore] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    setForbidden(false);
    try {
      const data = await api.reports(filter || undefined, 1);
      setReports(data);
      setPage(1);
      setHasMore(data.length >= REPORT_PAGE_SIZE);
    } catch (e) {
      if (e instanceof ApiError && e.status === 403) {
        setForbidden(true);
      } else {
        setError(e instanceof ApiError ? e.message : "Falha ao carregar denúncias.");
      }
    } finally {
      setLoading(false);
    }
  }, [filter]);

  async function loadMore() {
    if (loadingMore || !hasMore) return;
    setLoadingMore(true);
    const next = page + 1;
    try {
      const data = await api.reports(filter || undefined, next);
      setReports((prev) => [...prev, ...data]);
      setPage(next);
      setHasMore(data.length >= REPORT_PAGE_SIZE);
    } catch (e) {
      setError(e instanceof ApiError ? e.message : "Falha ao carregar mais denúncias.");
    } finally {
      setLoadingMore(false);
    }
  }

  useEffect(() => {
    if (isAdminUser(user)) load();
    else {
      setLoading(false);
      setForbidden(true);
    }
  }, [load, user?.email]);

  async function resolve(id: string, action: "Dismissed" | "Warned" | "Banned") {
    setBusy(`${id}:${action}`);
    setError(null);
    try {
      await api.resolveReport(id, action, notes[id]?.trim() || undefined);
      await load();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : "Não foi possível resolver a denúncia.");
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

  const openCount = reports.filter((r) => r.Status === "Open").length;

  return (
    <div className="app-container">
      <div className="flex items-center justify-between mb-4 flex-wrap gap-2">
        <h1 className="text-2xl font-bold text-cream">
          Denúncias
          {openCount > 0 && (
            <span className="text-silver text-sm font-normal"> · {openCount} em aberto</span>
          )}
        </h1>
        <div className="flex gap-1 bg-smoke rounded-lg p-1 text-sm">
          {(["", "Open", "Resolved"] as const).map((s) => (
            <button
              key={s || "all"}
              onClick={() => setFilter(s)}
              className={`px-3 py-1.5 rounded-md transition-colors ${
                filter === s ? "bg-brand text-ink font-semibold" : "text-silver hover:text-cream"
              }`}
            >
              {s === "" ? "Todas" : s === "Open" ? "Em aberto" : "Resolvidas"}
            </button>
          ))}
        </div>
      </div>

      {error && (
        <p className="text-rosa text-sm mb-4">{error}</p>
      )}

      {loading ? (
        <p className="text-silver">Carregando…</p>
      ) : reports.length === 0 ? (
        <p className="text-silver">Nenhuma denúncia neste filtro.</p>
      ) : (
        <ul className="space-y-3">
          {reports.map((r) => {
            const isUser = r.TargetType === "User";
            const isOpen = r.Status === "Open";
            return (
              <li
                key={r.Id}
                className="bg-smoke rounded-xl border border-smoke p-4"
              >
                <div className="flex items-center gap-2 flex-wrap mb-2">
                  <span className="text-cream font-semibold">
                    {TARGET_LABEL[r.TargetType] ?? r.TargetType}
                  </span>
                  <span className="text-xs bg-charcoal text-silver rounded-full px-2 py-0.5">
                    {REASON_LABEL[r.Reason] ?? r.Reason}
                  </span>
                  <span
                    className={`text-xs rounded-full px-2 py-0.5 ${
                      isOpen
                        ? "bg-amber/20 text-amber"
                        : "bg-esmeralda/20 text-esmeralda"
                    }`}
                  >
                    {isOpen ? "Em aberto" : ACTION_LABEL[r.Action ?? "Dismissed"] ?? "Resolvida"}
                  </span>
                  <span className="text-xs text-silver ml-auto">
                    {timeAgo(r.CreatedAt)}
                  </span>
                </div>

                <div className="text-sm text-silver mb-2">
                  <span className="text-cream/80">{r.ReporterNome}</span> denunciou
                  {" "}
                  <span className="text-silver/70">
                    {r.TargetType === "Listing" ? (
                      <Link
                        to={`/listings/${r.TargetId}`}
                        className="text-esmeralda hover:underline"
                      >
                        {TARGET_LABEL[r.TargetType]?.toLowerCase()} #{r.TargetId.slice(0, 8)}
                      </Link>
                    ) : (
                      <>
                        {TARGET_LABEL[r.TargetType]?.toLowerCase()} #{r.TargetId.slice(0, 8)}
                      </>
                    )}
                  </span>
                </div>

                {r.Details && (
                  <p className="text-sm text-cream/90 bg-charcoal rounded-lg p-2 mb-2 whitespace-pre-wrap">
                    {r.Details}
                  </p>
                )}

                {!isOpen && r.ResolvedBy && (
                  <p className="text-xs text-silver mb-1">
                    Resolvido por <span className="text-cream/80">{r.ResolvedBy}</span>
                    {r.ResolutionNote ? ` — ${r.ResolutionNote}` : ""}
                  </p>
                )}

                {isOpen && (
                  <div className="mt-2 flex flex-col gap-2">
                    <input
                      type="text"
                      value={notes[r.Id] ?? ""}
                      onChange={(e) =>
                        setNotes((n) => ({ ...n, [r.Id]: e.target.value }))
                      }
                      placeholder="Nota interna (opcional)…"
                      className="w-full bg-charcoal text-cream rounded-lg border border-smoke focus:border-esmeralda px-3 py-1.5 outline-none text-sm"
                    />
                    <div className="flex gap-2 flex-wrap">
                      <button
                        type="button"
                        onClick={() => resolve(r.Id, "Dismissed")}
                        disabled={busy !== null}
                        className="text-sm px-3 py-1.5 rounded-lg bg-charcoal text-silver hover:text-cream disabled:opacity-60"
                      >
                        {busy === `${r.Id}:Dismissed` ? "…" : "Arquivar"}
                      </button>
                      <button
                        type="button"
                        onClick={() => resolve(r.Id, "Warned")}
                        disabled={busy !== null}
                        className="text-sm px-3 py-1.5 rounded-lg bg-amber/80 text-ink font-medium disabled:opacity-60"
                      >
                        {busy === `${r.Id}:Warned` ? "…" : "Avisar"}
                      </button>
                      {isUser && (
                        <button
                          type="button"
                          onClick={() => resolve(r.Id, "Banned")}
                          disabled={busy !== null}
                          className="text-sm px-3 py-1.5 rounded-lg bg-rosa text-ink font-semibold disabled:opacity-60"
                        >
                          {busy === `${r.Id}:Banned` ? "…" : "Banir"}
                        </button>
                      )}
                    </div>
                  </div>
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
