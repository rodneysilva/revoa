import { useEffect, useState } from "react";
import { Link, NavLink, Outlet, useLocation } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import { isAdminEmail } from "../lib/admin";

const navItems = [
  { to: "/feed", label: "Feed" },
  { to: "/explore", label: "Explorar" },
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
  const admin = isAdminEmail(user?.email);

  useEffect(() => {
    setOpen(false);
  }, [location.pathname]);

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

          <nav className="hidden md:flex items-center gap-1">
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

          <div className="ml-auto flex items-center gap-3">
            {user ? (
              <>
                {user.verified && (
                  <span className="hidden sm:inline-flex items-center gap-1 text-xs font-semibold text-esmeralda bg-esmeralda/10 px-2 py-1 rounded-full">
                    ✓ Verificado
                  </span>
                )}
                <Link
                  to="/profile"
                  className="hidden sm:inline text-sm text-cream hover:text-esmeralda truncate max-w-[12ch]"
                >
                  {user.nome || "Perfil"}
                </Link>
              </>
            ) : (
              <>
                <Link
                  to="/login"
                  className="hidden sm:inline text-sm text-silver hover:text-cream"
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

            <button
              type="button"
              className="md:hidden inline-flex items-center justify-center w-10 h-10 rounded-lg text-cream hover:bg-smoke"
              aria-label={open ? "Fechar menu" : "Abrir menu"}
              aria-expanded={open}
              onClick={() => setOpen((o) => !o)}
            >
              {open ? "✕" : "≡"}
            </button>
          </div>
        </div>

        {open && (
          <div className="md:hidden border-t border-smoke bg-ink/95 backdrop-blur">
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
            <Link to="/explore" className="hover:text-cream">
              Explorar
            </Link>
            <Link to="/community" className="hover:text-cream">
              Comunidade
            </Link>
            <Link to="/transparency" className="hover:text-cream">
              Transparência
            </Link>
            <Link to="/login" className="hover:text-cream">
              Entrar
            </Link>
            <span className="hidden sm:inline">
              RM$ = crédito de troca, não cripto
            </span>
          </div>
        </div>
      </footer>
    </div>
  );
}
