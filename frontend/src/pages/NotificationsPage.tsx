import { useCallback, useEffect, useState } from "react";
import { ApiError, api } from "../api/client";
import { timeAgo } from "../lib/time";
import type { AppNotification } from "../api/types";

// Ícone por tipo de notificação (fallback: sino).
const TYPE_ICON: Record<string, string> = {
  EscrowUpdate: "🔒",
  Offer: "🤝",
  Transfer: "💱",
  Post: "📌",
  Chat: "💬",
  Donation: "🎁",
  Price: "💲",
  Help: "🤲",
  System: "🛠️",
};

const BTN_SEC =
  "bg-smoke text-cream text-xs font-semibold px-3 py-1.5 rounded-lg disabled:opacity-60 border border-smoke";

export function NotificationsPage() {
  const [items, setItems] = useState<AppNotification[] | null>(null);
  const [unread, setUnread] = useState(0);
  const [erro, setErro] = useState<string | null>(null);
  const [marcando, setMarcando] = useState<string | null>(null);

  const carregar = useCallback(async () => {
    setErro(null);
    try {
      const [lista, count] = await Promise.all([
        api.notifications(),
        api.unreadCount().catch(() => 0),
      ]);
      setItems(lista);
      setUnread(count);
    } catch (e) {
      setErro(e instanceof ApiError ? e.message : "Falha ao carregar notificações.");
    }
  }, []);

  useEffect(() => {
    void carregar();
  }, [carregar]);

  const marcarLida = useCallback(
    async (id: string) => {
      setMarcando(id);
      try {
        await api.markNotificationRead(id);
        setItems((atual) =>
          (atual ?? []).map((n) =>
            n.Id === id ? { ...n, Read: true, ReadAt: new Date().toISOString() } : n
          )
        );
        setUnread((u) => Math.max(0, u - 1));
      } catch (e) {
        setErro(e instanceof ApiError ? e.message : "Falha ao marcar como lida.");
      } finally {
        setMarcando(null);
      }
    },
    []
  );

  return (
    <div className="app-container">
      <div className="app-read">
        <header className="flex items-center justify-between gap-3 mb-6">
          <h1 className="text-2xl font-bold text-cream">Notificações</h1>
          {unread > 0 && (
            <span
              className="bg-rosa/15 text-rosa text-xs font-bold px-2.5 py-1 rounded-full border border-rosa/40"
              aria-label={`${unread} não lidas`}
            >
              {unread} não lida{unread > 1 ? "s" : ""}
            </span>
          )}
        </header>

        {erro && (
          <p className="text-rosa text-sm mb-4" role="alert">
            {erro}
          </p>
        )}

        {items === null && !erro && (
          <p className="text-silver text-sm">Carregando…</p>
        )}

        {items !== null && items.length === 0 && (
          <p className="text-silver text-sm">
            Nenhuma notificação por aqui — avisos de trocas, doações e comunidades
            aparecem nesta página.
          </p>
        )}

        {items !== null && items.length > 0 && (
          <ul className="space-y-3">
            {items.map((n) => (
              <li
                key={n.Id}
                className={`rounded-xl border p-4 ${
                  n.Read
                    ? "bg-smoke/30 border-smoke/50"
                    : "bg-smoke/60 border-smoke"
                }`}
              >
                <div className="flex items-start justify-between gap-3">
                  <div className="flex items-start gap-3 min-w-0">
                    <span className="text-xl leading-none mt-0.5" aria-hidden>
                      {TYPE_ICON[n.Type] ?? "🔔"}
                    </span>
                    <div className="min-w-0">
                      <p
                        className={`text-sm font-semibold ${
                          n.Read ? "text-silver" : "text-cream"
                        }`}
                      >
                        {n.Title}
                      </p>
                      <p className="text-sm text-silver/90 break-words">{n.Body}</p>
                      <p className="text-xs text-silver/60 mt-1">
                        {timeAgo(n.CreatedAt)}
                      </p>
                    </div>
                  </div>
                  {!n.Read && (
                    <button
                      type="button"
                      className={BTN_SEC}
                      disabled={marcando === n.Id}
                      onClick={() => void marcarLida(n.Id)}
                    >
                      {marcando === n.Id ? "…" : "Marcar lida"}
                    </button>
                  )}
                </div>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}
