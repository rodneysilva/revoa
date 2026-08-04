import { Link, NavLink, Outlet } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";

const navItems = [
  { to: "/feed", label: "Feed" },
  { to: "/community", label: "Comunidade" },
  { to: "/listings/new", label: "Anunciar" },
];

function navClass(active: boolean): string {
  return `px-3 py-2 rounded-lg text-sm font-medium transition ${
    active ? "text-esmeralda" : "text-silver hover:text-cream"
  }`;
}

export function Layout() {
  const { user } = useAuth();
  return (
    <div className="min-h-screen flex flex-col">
      <header className="sticky top-0 z-30 bg-ink/85 backdrop-blur border-b border-smoke">
        <div className="mx-auto w-full max-w-screen-2xl px-4 sm:px-6 lg:px-8 h-16 flex items-center gap-4">
          <Link to="/" className="wordmark text-2xl shrink-0" aria-label="revoa.me início">
            revoa.me
          </Link>
          <nav className="hidden sm:flex items-center gap-1">
            {navItems.map((n) => (
              <NavLink key={n.to} to={n.to} className={({ isActive }) => navClass(isActive)}>
                {n.label}
              </NavLink>
            ))}
          </nav>
          <div className="ml-auto flex items-center gap-3">
            {user ? (
              <>
                {user.verified && (
                  <span className="hidden sm:inline-flex items-center gap-1 text-xs font-semibold text-esmeralda bg-esmeralda/10 px-2 py-1 rounded-full">
                    ✓ Verificado
                  </span>
                )}
                <span className="rms text-cream text-sm hidden sm:inline">RM$ —</span>
                <Link to="/profile" className="text-sm text-cream hover:text-esmeralda truncate max-w-[10ch]">
                  {user.nome || "Perfil"}
                </Link>
              </>
            ) : (
              <>
                <Link to="/login" className="text-sm text-silver hover:text-cream">
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
          </div>
        </div>
        {/* Nav mobile */}
        <nav className="sm:hidden flex items-center gap-1 px-4 sm:px-6 lg:px-8 pb-2 overflow-x-auto">
          {navItems.map((n) => (
            <NavLink
              key={n.to}
              to={n.to}
              className={({ isActive }) =>
                `px-3 py-1.5 rounded-lg text-xs font-medium ${
                  isActive ? "text-esmeralda" : "text-silver"
                }`
              }
            >
              {n.label}
            </NavLink>
          ))}
        </nav>
      </header>

      <main className="flex-1 w-full max-w-screen-2xl mx-auto px-4 sm:px-6 lg:px-8">
        <Outlet />
      </main>

      <footer className="mt-auto border-t border-smoke py-6 text-center text-xs text-silver">
        revoa.me · Economia circular · sem fins lucrativos
      </footer>
    </div>
  );
}
