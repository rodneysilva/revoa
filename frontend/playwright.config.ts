import { defineConfig, devices } from "@playwright/test";

// Playwright E2E do frontend revoa.me. Sobe o Vite dev server automaticamente.
// Pré-req (uma vez): npm run test:e2e:install  (baixa o Chromium).
// Rodar:                npm run test:e2e        (precisa do backend .NET na :8000 para os dados).
export default defineConfig({
  testDir: "./e2e",
  fullyParallel: true,
  // AGENTS.md: e2e autenticados compartilham carteira/estado → workers:1 no CI.
  workers: process.env.CI ? 1 : undefined,
  retries: process.env.CI ? 2 : 0,
  reporter: process.env.CI ? "github" : "list",
  use: {
    baseURL: "http://localhost:5173",
    trace: "on-first-retry",
    locale: "pt-BR",
  },
  projects: [
    { name: "chromium", use: { ...devices["Desktop Chrome"] } },
  ],
  webServer: {
    command: "npm run dev",
    url: "http://localhost:5173",
    reuseExistingServer: !process.env.CI,
    timeout: 60_000,
  },
});
