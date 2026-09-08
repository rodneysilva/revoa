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

  // Web Push neste dispositivo: "unknown" até checar suporte + chave VAPID do
  // ambiente + inscrição existente. Estados em que o card nem aparece: navegador
  // sem Push API ou push desativado no backend (sem chaves VAPID configuradas).
  const [pushState, setPushState] = useState<
    "unknown" | "unsupported" | "off" | "on" | "disabled"
  >("unknown");
  const [pushBusy, setPushBusy] = useState(false);
  const [pushErro, setPushErro] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    void (async () => {
      const supported =
        typeof Notification !== "undefined" &&
        "serviceWorker" in navigator &&
        "PushManager" in window;
      if (!supported) {
        setPushState("unsupported");
        return;
      }
      try {
        const { PublicKey } = await api.pushKey();
        if (!PublicKey) {
          setPushState("disabled");
          return;
        }
        const reg = await navigator.serviceWorker.ready;
        const sub = await reg.pushManager.getSubscription();
        if (active) setPushState(sub ? "on" : "off");
      } catch {
        if (active) setPushState("off");
      }
    })();
    return () => {
      active = false;
    };
  }, []);

  async function ativarPush() {
    setPushBusy(true);
    setPushErro(null);
    try {
      const { PublicKey } = await api.pushKey();
      if (!PublicKey) {
        throw new Error("Notificações push não estão ativas neste ambiente.");
      }
      const perm = await Notification.requestPermission();
      if (perm !== "granted") {
        throw new Error("Permissão de notificação negada no navegador.");
      }
      const reg = await navigator.serviceWorker.ready;
      const sub = await reg.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: urlB64ToUint8Array(PublicKey),
      });
      const json = sub.toJSON();
      await api.pushSubscribe({
        Endpoint: json.endpoint ?? sub.endpoint,
        P256dh: json.keys?.p256dh ?? "",
        Auth: json.keys?.auth ?? "",
      });
      setPushState("on");
    } catch (e) {
      setPushErro(
        e instanceof ApiError || e instanceof Error
          ? e.message
          : "Falha ao ativar notificações."
      );
    } finally {
      setPushBusy(false);
    }
  }

  async function desativarPush() {
    setPushBusy(true);
    setPushErro(null);
    try {
      const reg = await navigator.serviceWorker.ready;
      const sub = await reg.pushManager.getSubscription();
      if (sub) {
        await api.pushUnsubscribe(sub.endpoint);
        await sub.unsubscribe();
      }
      setPushState("off");
    } catch (e) {
      setPushErro(
        e instanceof ApiError || e instanceof Error
          ? e.message
          : "Falha ao desativar notificações."
      );
    } finally {
      setPushBusy(false);
    }
  }

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

        {(pushState === "off" || pushState === "on") && (
          <div className="rounded-xl border p-4 mb-6 bg-smoke/30 border-smoke/50 flex items-center justify-between gap-3 flex-wrap">
            <div className="min-w-0">
              <p className="text-sm font-semibold text-cream flex items-center gap-2">
                <Bell aria-hidden className="w-4 h-4" />
                Notificações no dispositivo
              </p>
              <p className="text-xs text-silver/80 mt-0.5">
                {pushState === "on"
                  ? "Ativado — você recebe avisos mesmo com a revoa fechada."
                  : "Receba avisos de trocas, doações e comunidades mesmo com o site fechado."}
              </p>
              {pushErro && (
                <p className="text-xs text-rosa mt-1" role="alert">
                  {pushErro}
                </p>
              )}
            </div>
            <button
              type="button"
              disabled={pushBusy}
              onClick={() => (pushState === "on" ? void desativarPush() : void ativarPush())}
              className={
                pushState === "on"
                  ? "text-sm text-silver hover:text-rosa disabled:opacity-50 px-3 py-1.5 rounded-lg border border-smoke"
                  : "bg-brand text-ink text-sm font-semibold px-4 py-2 rounded-lg hover:opacity-90 disabled:opacity-50"
              }
            >
              {pushBusy ? "Aguarde…" : pushState === "on" ? "Desativar" : "Ativar notificações"}
            </button>
          </div>
        )}

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

// Chave pública VAPID (base64url) → Uint8Array p/ o pushManager.subscribe
// (applicationServerKey não aceita string base64url direto).
function urlB64ToUint8Array(base64Url: string): Uint8Array<ArrayBuffer> {
  const padding = "=".repeat((4 - (base64Url.length % 4)) % 4);
  const base64 = (base64Url + padding).replace(/-/g, "+").replace(/_/g, "/");
  const raw = atob(base64);
  // ArrayBuffer explícito: o DOM exige BufferSource com ArrayBuffer (não ArrayBufferLike).
  const output = new Uint8Array(new ArrayBuffer(raw.length));
  for (let i = 0; i < raw.length; i++) {
    output[i] = raw.charCodeAt(i);
  }
  return output;
}
