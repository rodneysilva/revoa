import { useSyncExternalStore } from "react";
import { api } from "../api/client";

// Contador global de notificações não lidas, compartilhado entre o badge do
// sino (Layout) e a página de Notificações (que zera ao abrir). Store mínimo
// via useSyncExternalStore — sem context/prop-drilling.
let count = 0;
const listeners = new Set<() => void>();

function emit() {
  listeners.forEach((l) => l());
}

export function setUnread(n: number) {
  if (n === count) return;
  count = n;
  emit();
}

// Re-busca o contador (badge é decorativo: falha em silêncio).
export async function refreshUnread() {
  try {
    setUnread(await api.unreadCount());
  } catch {
    /* mantém o valor atual */
  }
}

export function useUnread(): number {
  return useSyncExternalStore(
    (cb) => {
      listeners.add(cb);
      return () => listeners.delete(cb);
    },
    () => count
  );
}

// Marca as notificações informadas como lidas e zera o badge.
export async function markAllRead(ids: string[]) {
  await Promise.all(ids.map((id) => api.markNotificationRead(id)));
  setUnread(0);
}
