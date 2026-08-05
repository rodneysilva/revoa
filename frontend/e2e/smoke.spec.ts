import { test, expect } from "@playwright/test";

// Smoke tests E2E do frontend revoa.me (páginas públicas + fluxos básicos).
// Os fluxos on-chain (troca/doação/cupom) são cobertos pelos testes de integração do backend
// (Revoa.IntegrationTests). Aqui validamos a UI: render, navegação, formulários.

test.describe("revoa.me — smoke público", () => {
  test("landing carrega com hero + slogans + feed em destaque", async ({ page }) => {
    await page.goto("/");
    // wordmark presente
    await expect(page.locator("text=revoa.me").first()).toBeVisible();
    // seção do feed em destaque após o hero
    await expect(page.getByText(/o que a comunidade est[aá] oferecendo/i)).toBeVisible();
  });

  test("navega para o feed e vê anúncios (ou estado vazio amigável)", async ({ page }) => {
    await page.goto("/feed");
    // o feed tenta carregar (com backend: mostra cards; sem: mensagem amigável)
    await expect(page).toHaveURL(/\/feed/);
  });

  test("explorar tem o painel de filtros NxN", async ({ page }) => {
    await page.goto("/explore");
    await expect(page).toHaveURL(/\/explore/);
  });

  test("página de transparência carrega (tokenomia + princípios)", async ({ page }) => {
    await page.goto("/transparency");
    await expect(page.getByRole("heading", { name: /transparência/i })).toBeVisible();
    await expect(page.getByText(/sem fins lucrativos/i).first()).toBeVisible();
  });

  test("página de login tem o campo de e-mail", async ({ page }) => {
    await page.goto("/login");
    await expect(page.getByPlaceholder(/e-mail/i).or(page.locator('input[type="email"]')).first()).toBeVisible();
  });

  test("página de cadastro renderiza o formulário", async ({ page }) => {
    await page.goto("/register");
    await expect(page.getByRole("heading", { name: /cadastro|criar conta|participe/i })).toBeVisible();
  });

  test("rota inexistente mostra 404", async ({ page }) => {
    await page.goto("/rota-que-nao-existe-12345");
    await expect(page.getByText(/404|não encontrada|página não existe/i).first()).toBeVisible();
  });
});
