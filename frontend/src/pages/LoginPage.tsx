import { useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import { ApiError, api } from "../api/client";
import { useAuth } from "../auth/AuthContext";

const inputCls =
  "mt-1 w-full bg-smoke text-cream rounded-lg border border-smoke focus:border-esmeralda px-4 py-2.5 outline-none";

export function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setLoading(true);
    setError(null);
    try {
      const res = await api.login(email.trim());
      login(res.Token);
      navigate("/");
    } catch (e) {
      setError(e instanceof ApiError ? e.message : "Falha no login.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="mx-auto max-w-md px-4 py-16">
      <h1 className="text-3xl font-bold text-cream mb-2">Entrar</h1>
      <div className="bg-amber/10 border border-amber/40 text-amber rounded-lg p-3 text-sm mb-6">
        ⚠ Login exclusivo do ambiente de desenvolvimento: use um e-mail já cadastrado e
        verificado.
      </div>
      <form onSubmit={submit} className="space-y-4">
        <label className="block">
          <span className="text-sm text-silver">E-mail</span>
          <input
            type="email"
            required
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            className={inputCls}
            placeholder="voce@exemplo.com"
          />
        </label>
        {error && <p className="text-rosa text-sm">{error}</p>}
        <button
          disabled={loading}
          className="w-full bg-brand text-ink font-semibold px-6 py-3 rounded-xl disabled:opacity-60"
        >
          {loading ? "Entrando…" : "Entrar"}
        </button>
      </form>
      <p className="mt-6 text-sm text-silver text-center">
        Não tem conta?{" "}
        <Link to="/register" className="text-esmeralda hover:underline">
          Cadastre-se
        </Link>
      </p>
    </div>
  );
}
