// Handler de Web Push do service worker (RFC 8030). O SW do revoa.me é GERADO pelo
// workbox (generateSW), que não inclui listeners de push — este arquivo é injetado
// via workbox.importScripts no vite.config.ts e adiciona os dois eventos do ciclo:
//
//   push             → mensagem cifrada entregue pelo push service; o payload segue o
//                      shape emitido pelo backend: { title, body, payload }
//                      (payload é string JSON opcional com contexto da notificação).
//   notificationclick → foca a janela aberta (navegando para a url da notificação) ou
//                      abre uma nova.
//
// userVisibleOnly: toda notificação recebida TEM que ser exibida — sem isso o Chrome
// encerra a inscrição (regra do contrato do push manager).

self.addEventListener("push", (event) => {
  let data = {};
  try {
    data = event.data ? event.data.json() : {};
  } catch {
    data = { title: "revoa.me", body: event.data ? event.data.text() : "" };
  }

  let url = "/notifications";
  if (typeof data.payload === "string" && data.payload) {
    try {
      const payload = JSON.parse(data.payload);
      if (payload && typeof payload.url === "string") {
        url = payload.url;
      }
    } catch {
      /* payload não-JSON: segue para /notifications */
    }
  } else if (data.payload && typeof data.payload.url === "string") {
    url = data.payload.url;
  }

  event.waitUntil(
    self.registration.showNotification(data.title || "revoa.me", {
      body: typeof data.body === "string" ? data.body : "",
      icon: "/icon.svg",
      badge: "/icon.svg",
      tag: "revoa-notification",
      renotify: true,
      data: { url },
    })
  );
});

self.addEventListener("notificationclick", (event) => {
  event.notification.close();
  const url = (event.notification.data && event.notification.data.url) || "/notifications";

  event.waitUntil(
    self.clients
      .matchAll({ type: "window", includeUncontrolled: true })
      .then((windowClients) => {
        for (const client of windowClients) {
          if ("focus" in client) {
            if (client.url && "navigate" in client) {
              client.navigate(url).catch(() => {});
            }
            return client.focus();
          }
        }
        return self.clients.openWindow(url);
      })
  );
});
