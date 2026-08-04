import { Link, Navigate, Outlet } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";

// Gate de ação: exige login + verificação (e-mail e telefone).
export function ProtectedRoute() {
  const { user } = useAuth();
  if (!user) return <Navigate to="/login" replace />;
  if (!user.verified) {
    return (
      <div className="mx-auto max-w-2xl px-4 py-16 text-center">
        <div className="bg-amber/10 border border-amber/40 text-amber rounded-xl p-6">
          <h2 className="font-semibold text-lg mb-2">Quase lá!</h2>
          <p className="mb-4">Confirme seu e-mail e telefone para poder anunciar, trocar e doar.</p>
          <Link to="/register" className="text-amber underline">
            Retomar verificação
          </Link>
        </div>
      </div>
    );
  }
  return <Outlet />;
}
