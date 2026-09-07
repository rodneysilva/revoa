import { useCallback, useEffect, useState } from "react";
import { ApiError, api } from "../api/client";
import { timeAgo } from "../lib/time";
import { markAllRead } from "../lib/unread";
import type { AppNotification } from "../api/types";

import {
  ArrowLeftRight,
  Bell,
  CircleDollarSign,
  Gift,
  HandHeart,
  HeartHandshake,
  Lock,
  MessageCircle,
  Pin,
  Wrench,
  type LucideIcon,
} from "lucide-react";

// Ícone por tipo de notificação (fallback: sino).
const TYPE_ICON: Record<string, LucideIcon> = {
  EscrowUpdate: Lock,
  Offer: HeartHandshake,
  Transfer: ArrowLeftRight,
  Post: Pin,
  Chat: MessageCircle,
  Donation: Gift,
  Price: CircleDollarSign,
  Help: HandHeart,
  System: Wrench,
};

// Abrir a página já marca tudo como lido (o badge do sino zera na hora via
// store compartilhado). A lista é um a-conteceu, não uma fila a esvaziar.
export function NotificationsPage() {
  const [items, setItems] = useState<AppNotification[] | null>(null);
  const [erro, setErro] = useState<string | null>(null);

  const carregar = useCallback(async () => {
    setErro(null);
    try {
      const lista = await api.notifications();
      setItems(lista);

      const naoLidas = lista.filter((n) => !n.Read).map((n) => n.Id);
      if (naoLidas.length > 0) {
        try {
          await markAllRead(naoLidas);
          const agora = new Date().toISOString();
          setItems((atual) =>
            (atual ?? []).map((n) =>
              n.Read ? n : { ...n, Read: true, ReadAt: agora }
            )
          );
        } catch {
          /* abrir não pode falhar por causa do mark-read — a lista segue */
        }
      }
    } catch (e) {
      setErro(e instanceof ApiError ? e.message : "Falha ao carregar notificações.");
    }
  }, []);

  useEffect(() => {
    void carregar();
  }, [carregar]);

  return (
    <div className="app-container">
      <div className="app-read">
        <header className="flex items-center justify-between gap-3 mb-6">
          <h1 className="text-2xl font-bold text-cream">Notificações</h1>
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
                className="rounded-xl border p-4 bg-smoke/30 border-smoke/50"
              >
                <div className="flex items-start gap-3 min-w-0">
                  {(() => {
                    const TIcon = TYPE_ICON[n.Type] ?? Bell;
                    return (
                      <span className="text-silver mt-0.5" aria-hidden>
                        <TIcon className="w-4 h-4" />
                      </span>
                    );
                  })()}
                  <div className="min-w-0">
                    <p className="text-sm font-semibold text-cream">{n.Title}</p>
                    <p className="text-sm text-silver/90 break-words">{n.Body}</p>
                    <p className="text-xs text-silver/60 mt-1">{timeAgo(n.CreatedAt)}</p>
                  </div>
                </div>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}
