import { useEffect, useState } from "react";
import { Link, NavLink, Outlet, useLocation } from "react-router-dom";
import { Bell, Bookmark, Menu, Repeat, Wallet, X } from "lucide-react";
import { api } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import { isAdminUser } from "../lib/admin";
import { GlobalSearch } from "./GlobalSearch";
import { ThemeToggle } from "./ThemeToggle";
import { refreshUnread, useUnread } from "../lib/unread";

// Cortes empíricos do header (classes literais — o scanner do Tailwind não
// enxerga variantes compostas por template string). Medido com viewport real:
// <1024 só cabem wordmark + busca + hambúrguer (o cluster direito estoura até
// ~875px); 1024-1359 cabe o cluster, mas a nav de 4-5 links (+Admin do
// allowlist) só cabe a partir de ~1300px — por isso dois degraus:
//   lg (1024)  → cluster direito sai do menu e ganha a barra
//   1360px     → nav desktop substitui o hambúrguer

const navItems = [
  { to: "/feed", label: "Feed" },
  { to: "/listings", label: "Anúncios" },
  { to: "/community", label: "Comunidade" },
  { to: "/listings/new", label: "Anunciar" },
];

function navClass(active: boolean): string {
  return `px-3 py-2 rounded-lg text-sm font-medium transition ${
    active ? "text-esmeralda" : "text-silver hover:text-cream"
  }`;
}

export function Layout() {
  const { user, logout } = useAuth();
  const [open, setOpen] = useState(false);
  const location = useLocation();
  const admin = isAdminUser(user);
  const unread = useUnread();

  // Badge de não-lidas no sino — re-busca a cada rota (a página de Notificações
  // zera via store ao abrir). Degrada em silêncio, como o saldo.
  useEffect(() => {
    if (!user) return;
    void refreshUnread();
  }, [user, location.pathname]);

  // Saldo RM$ no header (VISUAL_IDENTITY §8). Degrada em silêncio: sem carteira
  // ou chain offline → "—" (nunca bloqueia a barra). Re-busca a cada rota (o saldo
  // muda após trocas).
  const [saldo, setSaldo] = useState<number | null>(null);
  useEffect(() => {
    if (!user) {
      setSaldo(null);
      return;
    }
    let active = true;
    api
      .walletBalance()
      .then((b) => {
        if (active) setSaldo(typeof b.Rvm === "number" ? b.Rvm : null);
      })
      .catch(() => {
        /* saldo é decorativo — falha escondida */
      });
    return () => {
      active = false;
    };
  }, [user, location.pathname]);

  return (
    <div className="app-shell">
      <header className="app-bar sticky top-0 z-40 bg-ink/85 backdrop-blur border-b border-smoke">
        <div className="app-bar-inner h-16 flex items-center gap-4">
          <Link
            to="/"
            className="wordmark text-2xl shrink-0"
            aria-label="revoa.me início"
          >
            revoa.me
          </Link>

          <nav className="hidden min-[1360px]:flex items-center gap-1">
            {navItems.map((n) => (
              <NavLink
                key={n.to}
                to={n.to}
                className={({ isActive }) => navClass(isActive)}
              >
                {n.label}
              </NavLink>
            ))}
            {admin && (
              <NavLink
                to="/admin"
                className={({ isActive }) => navClass(isActive)}
              >
                Admin
              </NavLink>
            )}
          </nav>

          <GlobalSearch />

          <div className="ml-auto flex items-center gap-3">
            {user ? (
              <>
                <Link
                  to="/saved"
                  className="hidden lg:inline-flex items-center gap-1 text-sm text-silver hover:text-amber"
                  title="Posts e anúncios salvos"
                >
                  <Bookmark aria-hidden className="w-4 h-4" />
                  Salvos
                </Link>
                <Link
                  to="/trades"
                  className="hidden lg:inline-flex items-center gap-1 text-sm text-silver hover:text-cream"
                  title="Minhas trocas"
                >
                  <Repeat aria-hidden className="w-4 h-4" />
                  Trocas
                </Link>
                <Link
                  to="/wallet"
                  className="hidden lg:inline-flex items-center gap-1.5 text-sm text-lima hover:text-cream"
                  title="Carteira — créditos de troca (RM$)"
                >
                  <span className="rms">RM$</span>
                  {saldo !== null
                    ? saldo.toLocaleString("pt-BR", { maximumFractionDigits: 2 })
                    : "—"}
                </Link>
                <Link
                  to="/notifications"
                  className="hidden lg:inline-flex items-center justify-center relative text-silver hover:text-cream"
                  title="Notificações"
                  aria-label={
                    unread > 0 ? `Notificações (${unread} não lidas)` : "Notificações"
                  }
                >
                  <Bell aria-hidden className="w-5 h-5" />
                  {unread > 0 && (
                    <span className="absolute -top-1.5 -right-2 min-w-[18px] h-[18px] px-1 flex items-center justify-center bg-rosa text-ink text-[10px] font-bold rounded-full border-2 border-ink">
                      {unread > 9 ? "9+" : unread}
                    </span>
                  )}
                </Link>
                <Link
                  to="/profile"
                  className="hidden lg:inline text-sm text-cream hover:text-esmeralda truncate max-w-[12ch]"
                >
                  {user.nome || "Perfil"}
                </Link>
                <button
                  type="button"
                  onClick={logout}
                  className="hidden lg:inline text-sm text-silver hover:text-rosa"
                  title="Encerrar sessão"
                >
                  Sair
                </button>
              </>
            ) : (
              <>
                <Link
                  to="/login"
                  className="hidden lg:inline text-sm text-silver hover:text-cream"
                >
                  Entrar
                </Link>
                <Link
                  to="/register"
                  className="bg-brand text-ink text-sm font-semibold px-4 py-2 rounded-lg"
                >
                  Cadastrar
                </Link>
              </>
            )}

            <ThemeToggle />

            <button
              type="button"
              className="min-[1360px]:hidden inline-flex items-center justify-center w-10 h-10 rounded-lg text-cream hover:bg-smoke"
              aria-label={open ? "Fechar menu" : "Abrir menu"}
              aria-expanded={open}
              onClick={() => setOpen((o) => !o)}
            >
              {open ? (
                <X aria-hidden className="w-5 h-5" />
              ) : (
                <Menu aria-hidden className="w-5 h-5" />
              )}
            </button>
          </div>
        </div>

        {open && (
          <div className="min-[1360px]:hidden border-t border-smoke bg-ink/95 backdrop-blur">
            <div className="app-bar-inner py-3 flex flex-col gap-1">
              {navItems.map((n) => (
                <NavLink
                  key={n.to}
                  to={n.to}
                  className={({ isActive }) =>
                    `px-3 py-2.5 rounded-lg text-sm font-medium transition ${
                      isActive
                        ? "text-esmeralda bg-smoke/60"
                        : "text-silver hover:text-cream"
                    }`
                  }
                >
                  {n.label}
                </NavLink>
              ))}
              {admin && (
                <NavLink
                  to="/admin"
                  className={({ isActive }) =>
                    `px-3 py-2.5 rounded-lg text-sm font-medium transition ${
                      isActive
                        ? "text-esmeralda bg-smoke/60"
                        : "text-silver hover:text-cream"
                    }`
                  }
                >
                  Admin
                </NavLink>
              )}
              {!user && (
                <div className="mt-1 pt-2 border-t border-smoke flex flex-col gap-1">
                  <Link
                    to="/login"
                    className="px-3 py-2.5 rounded-lg text-sm text-silver hover:text-cream"
                  >
                    Entrar
                  </Link>
                  <Link
                    to="/register"
                    className="px-3 py-2.5 rounded-lg text-sm text-esmeralda font-semibold"
                  >
                    Cadastrar
                  </Link>
                </div>
              )}
              {user && (
                <div className="mt-1 pt-2 border-t border-smoke flex flex-col gap-1">
                  <Link
                    to="/profile"
                    className="px-3 py-2.5 rounded-lg text-sm text-silver hover:text-cream"
                  >
                    Perfil
                  </Link>
                  <Link
                    to="/saved"
                    className="px-3 py-2.5 rounded-lg text-sm text-silver hover:text-cream flex items-center gap-2"
                  >
                    <Bookmark aria-hidden className="w-4 h-4" />
                    Salvos
                  </Link>
                  <Link
                    to="/wallet"
                    className="px-3 py-2.5 rounded-lg text-sm text-silver hover:text-cream flex items-center gap-2"
                  >
                    <Wallet aria-hidden className="w-4 h-4" />
                    Carteira
                  </Link>
                  <Link
                    to="/notifications"
                    className="px-3 py-2.5 rounded-lg text-sm text-silver hover:text-cream flex items-center gap-2"
                  >
                    <Bell aria-hidden className="w-4 h-4" />
                    Notificações
                    {unread > 0 && (
                      <span className="bg-rosa text-ink text-[10px] font-bold px-1.5 py-0.5 rounded-full">
                        {unread > 9 ? "9+" : unread}
                      </span>
                    )}
                  </Link>
                  <Link
                    to="/trades"
                    className="px-3 py-2.5 rounded-lg text-sm text-silver hover:text-cream"
                  >
                    Minhas trocas
                  </Link>
                  <button
                    type="button"
                    onClick={() => { logout(); setOpen(false); }}
                    className="px-3 py-2.5 rounded-lg text-sm text-left text-rosa hover:text-cream"
                  >
                    Sair
                  </button>
                </div>
              )}
            </div>
          </div>
        )}
      </header>

      <main className="flex-1 w-full">
        <Outlet />
      </main>

      <footer className="app-bar mt-auto border-t border-smoke">
        <div className="app-bar-inner py-6 flex flex-col sm:flex-row gap-2 justify-between items-center text-xs text-silver">
          <span>revoa.me · Economia circular · sem fins lucrativos</span>
          <div className="flex items-center gap-3">
            <Link to="/feed" className="hover:text-cream">
              Feed
            </Link>
            <Link to="/listings" className="hover:text-cream">
              Anúncios
            </Link>
            <Link to="/community" className="hover:text-cream">
              Comunidade
            </Link>
            <Link to="/transparency" className="hover:text-cream">
              Transparência
            </Link>
            <Link to="/terms" className="hover:text-cream">
              Termos de Uso
            </Link>
            {!user && (
              <Link to="/login" className="hover:text-cream">
                Entrar
              </Link>
            )}
            <span className="hidden sm:inline">
              RM$ = crédito de troca, não cripto
            </span>
          </div>
        </div>
      </footer>
    </div>
  );
}
