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

export function RegisterPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [step, setStep] = useState<"form" | "verify">("form");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

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
    if (!ageOk(birthDate)) {
      setError("É preciso ter 18 anos ou mais para participar.");
      return;
    }
    setLoading(true);
    try {
      const res = await api.register({
        Nome: nome.trim(),
        Email: email.trim(),
        Telefone: telefone.trim(),
        BirthDate: birthDate,
        CouponCode: coupon.trim() || undefined,
      });
      setUserId(res.UserId);
      setNeedsEmail(res.NeedsEmailVerification);
      setNeedsPhone(res.NeedsPhoneVerification);
      setStep("verify");
    } catch (e) {
      setError(e instanceof ApiError ? e.message : "Falha no cadastro.");
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
      const res = await api.login(email.trim());
      login(res.Token);
      navigate("/");
    } catch (e) {
      setError(
        e instanceof ApiError
          ? e.message
          : "Verificação concluída, mas falha no login. Tente a tela de Entrar."
      );
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="mx-auto max-w-md px-4 py-12">
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
              value={telefone}
              onChange={(e) => setTelefone(e.target.value)}
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
  );
}
