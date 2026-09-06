import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { api } from "../api/client";
import { useAuth } from "../auth/AuthContext";

const DISMISS_KEY = "revoa.onboarding.dismissed";

// Checklist das primeiras ações (conta nova). Cada passo usa dado que já existe
// (claim verified, feed próprio, comunidades) — quando tudo completa, some.
// Dispensável: guarda no localStorage e não volta a incomodar.
export function OnboardingChecklist() {
  const { user } = useAuth();
  const [dismissed, setDismissed] = useState(true); // só mostra depois de checar storage
  const [temAnuncio, setTemAnuncio] = useState<boolean | null>(null);
  const [temComunidade, setTemComunidade] = useState<boolean | null>(null);

  useEffect(() => {
    setDismissed(localStorage.getItem(DISMISS_KEY) === "1");
  }, []);

  useEffect(() => {
    if (!user) return;
    let active = true;
    api
      .feed({ sellerIds: user.userId, page: 1 })
      .then((r) => active && setTemAnuncio(r.length > 0))
      .catch(() => active && setTemAnuncio(false));
    api
      .userCommunities(user.userId)
      .then((r) => active && setTemComunidade(r.length > 0))
      .catch(() => active && setTemComunidade(false));
    return () => {
      active = false;
    };
  }, [user]);

  if (!user || dismissed) return null;

  const passos: { done: boolean; label: string; cta?: { to: string; text: string } }[] = [
    {
      done: user.verified,
      label: "Confirme seu e-mail e telefone",
      cta: user.verified ? undefined : { to: "/profile", text: "Verificar" },
    },
    {
      done: temAnuncio === true,
      label: "Crie seu primeiro anúncio",
      cta: user.verified ? { to: "/listings/new", text: "Anunciar" } : undefined,
    },
    {
      done: temComunidade === true,
      label: "Entre em uma comunidade do seu bairro",
      cta: { to: "/community", text: "Descobrir" },
    },
  ];

  // Ainda carregando os dois primeiros sinais → não mostra esqueleto.
  if (temAnuncio === null || temComunidade === null) return null;

  const completos = passos.filter((p) => p.done).length;
  if (completos === passos.length) return null;

  function dismiss() {
    localStorage.setItem(DISMISS_KEY, "1");
    setDismissed(true);
  }

  return (
    <section
      className="mb-6 bg-charcoal border border-smoke rounded-2xl p-5"
      aria-label="Primeiros passos"
    >
      <div className="flex items-start justify-between gap-3 mb-3">
        <div>
          <h2 className="text-cream font-bold">
            Bem-vindo{user.nome ? `, ${user.nome.split(" ")[0]}` : ""}! 👋
          </h2>
          <p className="text-xs text-silver mt-0.5">
            {completos} de {passos.length} primeiros passos
          </p>
        </div>
        <button
          type="button"
          onClick={dismiss}
          aria-label="Dispensar primeiros passos"
          className="text-silver hover:text-cream text-sm leading-none"
        >
          ✕
        </button>
      </div>

      {/* progresso */}
      <div className="flex gap-1.5 mb-4" aria-hidden>
        {passos.map((p, i) => (
          <span
            key={i}
            className={`h-1.5 flex-1 rounded-full ${p.done ? "bg-esmeralda" : "bg-smoke"}`}
          />
        ))}
      </div>

      <ul className="space-y-2">
        {passos.map((p) => (
          <li key={p.label} className="flex items-center gap-3 text-sm">
            <span
              className={`w-5 h-5 shrink-0 rounded-full flex items-center justify-center text-xs ${
                p.done ? "bg-esmeralda text-ink" : "border border-smoke text-transparent"
              }`}
              aria-hidden
            >
              ✓
            </span>
            <span className={p.done ? "text-silver line-through" : "text-cream"}>
              {p.label}
            </span>
            {!p.done && p.cta && (
              <Link
                to={p.cta.to}
                className="ml-auto text-esmeralda text-xs font-semibold hover:underline whitespace-nowrap"
              >
                {p.cta.text} →
              </Link>
            )}
          </li>
        ))}
      </ul>
    </section>
  );
}
