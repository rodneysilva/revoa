import { useState, type FormEvent } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { ApiError, api } from "../api/client";
import { useAuth } from "../auth/AuthContext";

const inputCls =
  "mt-1 w-full bg-smoke text-cream rounded-lg border border-smoke focus:border-esmeralda px-4 py-2.5 outline-none";

export function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const initialEmail =
    (location.state as { email?: string } | null)?.email?.trim() ?? "";

  const [step, setStep] = useState<"email" | "code">("email");
  const [email, setEmail] = useState(initialEmail);
  const [code, setCode] = useState("");
  const [loading, setLoading] = useState(false);
  const [info, setInfo] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  // Etapa 1: solicita o código de acesso (vai para o e-mail — em dev aparece nos logs do backend).
  async function requestCode(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setInfo(null);
    setLoading(true);
    try {
      const res = await api.loginRequest(email.trim());
      setInfo(res.message ?? "Se a conta existir, enviamos um código.");
      setStep("code");
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Falha ao solicitar o código.");
    } finally {
      setLoading(false);
    }
  }

  // DEV: entra direto pelo e-mail (atalho dev-only; em prod o /login direto retorna 404).
  // Existe porque, em desenvolvimento, o Postfix local não entrega e-mail em caixa real.
  async function directLogin() {
    if (!email.trim()) return;
    setError(null);
    setLoading(true);
    try {
      const res = await api.login(email.trim());
      login(res.Token);
      navigate("/");
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Falha no login.");
    } finally {
      setLoading(false);
    }
  }

  // Etapa 2: valida o código e entra.
  async function confirm(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      const res = await api.loginConfirm(email.trim(), code.trim());
      login(res.Token);
      navigate("/");
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Código inválido ou expirado.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="mx-auto max-w-md px-4 py-16">
      <h1 className="text-3xl font-bold text-cream mb-2">Entrar</h1>
      <p className="text-sm text-silver mb-6">
        Acesso sem senha. Você recebe um código no e-mail para confirmar que é você.
      </p>

      {step === "email" ? (
        <form onSubmit={requestCode} className="space-y-4">
          <label className="block">
            <span className="text-sm text-silver">E-mail</span>
            <input
              type="email"
              required
              autoFocus
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
            {loading ? "Enviando…" : "Receber código"}
          </button>

          {import.meta.env.DEV && (
            <div className="pt-2 border-t border-smoke">
              <p className="text-xs text-silver mb-2 text-center">
                Em desenvolvimento o SMTP não entrega o e-mail. Entre direto (dev-only):
              </p>
              <button
                type="button"
                disabled={loading || !email.trim()}
                onClick={directLogin}
                className="w-full bg-help text-ink font-semibold px-6 py-2.5 rounded-xl disabled:opacity-60"
              >
                Entrar direto (dev)
              </button>
            </div>
          )}
        </form>
      ) : (
        <form onSubmit={confirm} className="space-y-4">
          {info && (
            <div className="bg-amber/10 border border-amber/40 text-amber rounded-lg p-3 text-sm">
              {info}
              <div className="mt-1 text-xs opacity-80">
                Em desenvolvimento o código aparece nos <strong>logs do backend</strong> (MailKit
                DEV: “Código de login …”).
              </div>
            </div>
          )}
          <label className="block">
            <span className="text-sm text-silver">
              Código de acesso (enviado para {email})
            </span>
            <input
              required
              autoFocus
              inputMode="numeric"
              pattern="[0-9]{6}"
              maxLength={6}
              value={code}
              onChange={(e) => setCode(e.target.value.replace(/\D/g, ""))}
              className={inputCls}
              placeholder="000000"
            />
          </label>
          <div className="flex gap-2">
            <button
              type="button"
              onClick={() => {
                setStep("email");
                setCode("");
                setInfo(null);
                setError(null);
              }}
              className="px-4 py-3 rounded-xl bg-smoke text-silver border border-smoke"
            >
              Trocar e-mail
            </button>
            <button
              disabled={loading || code.length !== 6}
              className="flex-1 bg-brand text-ink font-semibold px-6 py-3 rounded-xl disabled:opacity-60"
            >
              {loading ? "Entrando…" : "Entrar"}
            </button>
          </div>
          <button
            type="button"
            disabled={loading}
            onClick={requestCode}
            className="text-sm text-sky hover:underline"
          >
              Reenviar código
          </button>
          {error && <p className="text-rosa text-sm">{error}</p>}
        </form>
      )}

      <p className="mt-6 text-sm text-silver text-center">
        Não tem conta?{" "}
        <Link to="/register" className="text-esmeralda hover:underline">
          Cadastre-se
        </Link>
      </p>
    </div>
  );
}
