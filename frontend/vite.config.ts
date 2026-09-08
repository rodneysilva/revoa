import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
import { VitePWA } from "vite-plugin-pwa";

// Vite config do SPA revoa.me.
// Proxy /api e /hubs → backend .NET (http://localhost:8000) resolve CORS na mesma origem.
// server.host 0.0.0.0 + allowedHosts p/ acesso LAN/túneis (cloudflared). PWA instalável.
export default defineConfig({
  plugins: [
    react(),
    tailwindcss(),
    VitePWA({
      registerType: "autoUpdate",
      injectRegister: "auto",
      workbox: {
        // Ativa o SW novo imediatamente (o controllerchange no main.tsx recarrega).
        skipWaiting: true,
        clientsClaim: true,
        // Listeners de Web Push (push/notificationclick) vivem fora do bundle gerado —
        // public/push-handler.js é copiado ao dist e importado pelo SW no runtime.
        importScripts: ["/push-handler.js"],
      },
      manifest: {
        name: "revoa.me",
        short_name: "revoa",
        description: "Economia circular e ajuda mútua com moeda social RVM.",
        theme_color: "#10b981",
        background_color: "#0a0a0a",
        display: "standalone",
        start_url: "/",
        lang: "pt-BR",
        icons: [
          {
            src: "/icon.svg",
            sizes: "any",
            type: "image/svg+xml",
            purpose: "any",
          },
        ],
      },
      devOptions: {
        enabled: true,
      },
    }),
  ],
  server: {
    host: "0.0.0.0",
    port: 5173,
    allowedHosts: true,
    proxy: {
      "/api": { target: "http://localhost:8000", changeOrigin: true },
      "/hubs": { target: "http://localhost:8000", ws: true, changeOrigin: true },
    },
  },
});
