import { useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import { ApiError, api } from "../api/client";
import { useAuth } from "../auth/AuthContext";

const inputCls =
  "mt-1 w-full bg-smoke text-cream rounded-lg border border-smoke focus:border-esmeralda px-4 py-2.5 outline-none";

function ageOk(dob: string): boolean {
  if (!dob) return false;
  const birth = new Date(dob);
  if (isNaN(birth.getTime())) return false;
  const age = (Date.now() - birth.getTime()) / (365.25 * 24 * 3600 * 1000);
  return age >= 18;
}

function formatPhone(raw: string): string {
  const d = raw.replace(/\D/g, "").slice(0, 11);
  if (d.length <= 2) return d.length ? `(${d}` : "";
  if (d.length <= 7) return `(${d.slice(0, 2)}) ${d.slice(2)}`;
  if (d.length <= 10) return `(${d.slice(0, 2)}) ${d.slice(2, 6)}-${d.slice(6)}`;
  return `(${d.slice(0, 2)}) ${d.slice(2, 7)}-${d.slice(7, 11)}`;
}

export function RegisterPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [step, setStep] = useState<"form" | "verify">("form");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  // Detecção inteligente: e-mail já cadastrado -> oferece login + reenvio de verificação.
  const [accountExists, setAccountExists] = useState(false);

  // Etapa 1 — cadastro
  const [nome, setNome] = useState("");
  const [email, setEmail] = useState("");
  const [telefone, setTelefone] = useState("");
  const [birthDate, setBirthDate] = useState("");
  const [coupon, setCoupon] = useState("");

  // Etapa 2 — verificação dupla
  const [userId, setUserId] = useState<string | null>(null);
  const [needsEmail, setNeedsEmail] = useState(false);
  const [needsPhone, setNeedsPhone] = useState(false);
  const [emailToken, setEmailToken] = useState("");
  const [phoneCode, setPhoneCode] = useState("");
  const [emailDone, setEmailDone] = useState(false);
  const [phoneDone, setPhoneDone] = useState(false);

  async function submitRegister(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setAccountExists(false);
    if (!ageOk(birthDate)) {
      setError("É preciso ter 18 anos ou mais para participar.");
      return;
    }
    setLoading(true);
    try {
      const res = await api.register({
        Name: nome.trim(),
        Email: email.trim(),
        Phone: telefone.trim(),
        BirthDate: birthDate,
        CouponCode: coupon.trim() || undefined,
      });
      setUserId(res.UserId);
      setNeedsEmail(res.NeedsEmailVerification);
      setNeedsPhone(res.NeedsPhoneVerification);
      setStep("verify");
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : "Falha no cadastro.";
      setError(msg);
      // Conta já existe -> ativa o caminho de recuperação de acesso.
      if (/já está cadastrado|recupere|cadastro/i.test(msg)) setAccountExists(true);
    } finally {
      setLoading(false);
    }
  }

  // Recuperação de acesso: reenvia verificação para a conta existente (passwordless — sem senha).
  async function recover() {
    if (!email.trim()) return;
    setLoading(true);
    setError(null);
    try {
      const res = await api.resendVerification(email.trim());
      setUserId(res.UserId);
      setNeedsEmail(res.NeedsEmailVerification);
      setNeedsPhone(res.NeedsPhoneVerification);
      setAccountExists(false);
      setStep("verify");
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Não foi possível reenviar a verificação.");
    } finally {
      setLoading(false);
    }
  }

  async function verify(kind: "email" | "phone") {
    if (!userId) return;
    setError(null);
    setLoading(true);
    try {
      if (kind === "email") {
        await api.verifyEmail(userId, emailToken.trim());
        setEmailDone(true);
      } else {
        await api.verifyPhone(userId, phoneCode.trim());
        setPhoneDone(true);
      }
    } catch (e) {
      setError(
        e instanceof ApiError
          ? e.message
          : `Falha ao verificar ${kind === "email" ? "e-mail" : "telefone"}.`
      );
    } finally {
      setLoading(false);
    }
  }

  const bothDone = (!needsEmail || emailDone) && (!needsPhone || phoneDone);

  async function finish() {
    if (!email.trim()) return;
    setLoading(true);
    setError(null);
    try {
      if (import.meta.env.DEV) {
        const res = await api.login(email.trim());
        login(res.Token);
        navigate("/");
      } else {
        navigate("/login", { state: { email: email.trim() } });
      }
    } catch (e) {
      setError(
        e instanceof ApiError
          ? e.message
          : "Verificação concluída. Use a tela de Entrar para acessar."
      );
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="app-container">
      <div className="app-read">
      <h1 className="text-3xl font-bold text-cream mb-6">Criar conta</h1>

      {step === "form" && (
        <form onSubmit={submitRegister} className="space-y-4">
          <label className="block">
            <span className="text-sm text-silver">Nome</span>
            <input
              required
              value={nome}
              onChange={(e) => setNome(e.target.value)}
              className={inputCls}
              placeholder="Seu nome"
            />
          </label>
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
          <label className="block">
            <span className="text-sm text-silver">Telefone / WhatsApp</span>
            <input
              required
              inputMode="tel"
              value={telefone}
              onChange={(e) => setTelefone(formatPhone(e.target.value))}
              className={inputCls}
              placeholder="(11) 90000-0000"
            />
          </label>
          <label className="block">
            <span className="text-sm text-silver">Data de nascimento</span>
            <input
              type="date"
              required
              value={birthDate}
              onChange={(e) => setBirthDate(e.target.value)}
              className={inputCls}
            />
          </label>
          <label className="block">
            <span className="text-sm text-silver">Cupom (opcional)</span>
            <input
              value={coupon}
              onChange={(e) => setCoupon(e.target.value)}
              className={inputCls}
              placeholder="Convidado por alguém?"
            />
          </label>
          {error && <p className="text-rosa text-sm">{error}</p>}

          {accountExists && (
            <div className="bg-amber/10 border border-amber/40 rounded-lg p-4 space-y-3">
              <p className="text-sm text-cream">
                Detectamos que <strong>{email}</strong> já tem conta. Como você acessa sem senha
                (passkey), recupere o acesso:
              </p>
              <div className="flex flex-col gap-2">
                <button
                  type="button"
                  disabled={loading}
                  onClick={() => navigate("/login", { state: { email: email.trim() } })}
                  className="w-full bg-brand text-ink font-semibold px-4 py-2 rounded-lg disabled:opacity-60"
                >
                  Fazer login
                </button>
                <button
                  type="button"
                  disabled={loading}
                  onClick={recover}
                  className="w-full bg-smoke text-cream font-semibold px-4 py-2 rounded-lg border border-smoke disabled:opacity-60"
                >
                  {loading ? "Reenviando…" : "Reenviar verificação (e-mail + telefone)"}
                </button>
              </div>
            </div>
          )}

          <button
            disabled={loading}
            className="w-full bg-brand text-ink font-semibold px-6 py-3 rounded-xl disabled:opacity-60"
          >
            {loading ? "Enviando…" : "Continuar"}
          </button>
        </form>
      )}

      {step === "verify" && (
        <div className="space-y-6">
          <div className="bg-sky/10 border border-sky/40 text-sky rounded-lg p-3 text-sm">
            Quase lá! Confirme seu e-mail e telefone. Em desenvolvimento, o token e o
            código OTP aparecem nos logs do backend (MailKit/Zenvia).
          </div>

          {needsEmail && (
            <div className={emailDone ? "opacity-60" : ""}>
              <label className="block">
                <span className="text-sm text-silver">Token de e-mail</span>
                <input
                  disabled={emailDone}
                  value={emailToken}
                  onChange={(e) => setEmailToken(e.target.value)}
                  className={inputCls}
                  placeholder="Cole o token recebido por e-mail"
                />
              </label>
              <button
                type="button"
                disabled={emailDone || loading}
                onClick={() => verify("email")}
                className="mt-2 w-full bg-brand text-ink font-semibold px-4 py-2 rounded-lg disabled:opacity-60"
              >
                {emailDone ? "✓ E-mail confirmado" : "Confirmar e-mail"}
              </button>
            </div>
          )}

          {needsPhone && (
            <div className={phoneDone ? "opacity-60" : ""}>
              <label className="block">
                <span className="text-sm text-silver">Código do telefone (OTP)</span>
                <input
                  disabled={phoneDone}
                  value={phoneCode}
                  onChange={(e) => setPhoneCode(e.target.value)}
                  className={inputCls}
                  placeholder="Cole o código recebido por SMS/WhatsApp"
                />
              </label>
              <button
                type="button"
                disabled={phoneDone || loading}
                onClick={() => verify("phone")}
                className="mt-2 w-full bg-brand text-ink font-semibold px-4 py-2 rounded-lg disabled:opacity-60"
              >
                {phoneDone ? "✓ Telefone confirmado" : "Confirmar telefone"}
              </button>
            </div>
          )}

          {error && <p className="text-rosa text-sm">{error}</p>}

          {bothDone && (
            <button
              type="button"
              disabled={loading}
              onClick={finish}
              className="w-full bg-help text-ink font-semibold px-6 py-3 rounded-xl disabled:opacity-60"
            >
              {loading ? "Entrando…" : "Entrar agora"}
            </button>
          )}
        </div>
      )}

      <p className="mt-6 text-sm text-silver text-center">
        Já tem conta?{" "}
        <Link to="/login" className="text-esmeralda hover:underline">
          Entrar
        </Link>
      </p>
      </div>
    </div>
  );
}
