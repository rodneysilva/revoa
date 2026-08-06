import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { api } from "../api/client";
import type { Category, PriceReference } from "../api/types";
import { useAuth } from "../auth/AuthContext";
import { brlEstimate } from "../lib/format";
import { useBrlRate } from "../lib/useBrlRate";

const COMO_FUNCIONA: { icon: string; text: string }[] = [
  {
    icon: "📝",
    text: "Você anuncia o que tem para oferecer — um produto ou um serviço.",
  },
  {
    icon: "🔄",
    text: "A cada troca, você usa RVM, o crédito de troca da plataforma.",
  },
  {
    icon: "💰",
    text: "Quando alguém pega o que você ofereceu, você recebe RVM.",
  },
  {
    icon: "🛒",
    text: "Quando você precisa de algo, usa seus RVM com outros membros.",
  },
];

function rvmFmt(n: number): string {
  return n.toLocaleString("pt-BR", { maximumFractionDigits: 4 });
}

function brlFmt(n: number): string {
  return new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency: "BRL",
    minimumFractionDigits: Number.isInteger(n) ? 0 : 2,
    maximumFractionDigits: 2,
  }).format(n);
}

function formatDate(iso?: string): string {
  if (!iso) return "—";
  try {
    return new Date(iso).toLocaleDateString("pt-BR", {
      day: "2-digit",
      month: "short",
      year: "numeric",
    });
  } catch {
    return iso;
  }
}

function prettifySlug(slug?: string): string {
  if (!slug) return "Categoria";
  return slug
    .replace(/[-_]+/g, " ")
    .split(" ")
    .filter(Boolean)
    .map((w) => w.charAt(0).toUpperCase() + w.slice(1))
    .join(" ");
}

export function TransparencyPage() {
  const { user } = useAuth();
  const { rate, disclaimer, loading: rateLoading } = useBrlRate();

  const [refs, setRefs] = useState<PriceReference[] | null>(null);
  const [cats, setCats] = useState<Category[]>([]);
  const [refsError, setRefsError] = useState(false);

  useEffect(() => {
    let active = true;
    api
      .pricing()
      .then((data) => {
        if (active) setRefs(data);
      })
      .catch(() => {
        if (active) {
          setRefsError(true);
          setRefs([]);
        }
      });
    api
      .categories()
      .then((data) => {
        if (active) setCats(data);
      })
      .catch(() => {
        /* nome amigável é best-effort — fallback p/ slug */
      });
    return () => {
      active = false;
    };
  }, []);

  const catName = useMemo(() => {
    const map = new Map<string, string>();
    for (const c of cats) map.set(c.Id, c.Nome);
    return (r: PriceReference) => map.get(r.CategoriaId) ?? prettifySlug(r.CategoriaSlug);
  }, [cats]);

  const ctaTo = user ? "/feed" : "/register";

  return (
    <div className="app-container">
      {/* Cabeçalho */}
      <header className="mb-10 sm:mb-12">
        <span className="inline-flex items-center gap-1.5 rounded-full bg-esmeralda/15 text-esmeralda text-xs font-semibold px-3 py-1 mb-4">
          🪙 moeda social RVM
        </span>
        <h1 className="text-3xl sm:text-4xl lg:text-5xl font-bold text-cream tracking-tight">
          Transparência
        </h1>
        <p className="mt-3 text-silver max-w-2xl text-base sm:text-lg">
          <span className="wordmark">revoa.me</span> é uma plataforma sem fins lucrativos para
          trocar produtos e serviços entre vizinhos. Aqui explicamos como funciona, sem letra
          miúda.
        </p>
        <Link
          to="/terms"
          className="inline-block mt-4 text-sm text-esmeralda hover:underline"
        >
          Ver Termos de Uso completos →
        </Link>
      </header>

      {/* Como funciona */}
      <section className="mb-12 sm:mb-16">
        <SectionHeading
          kicker="Como funciona"
          title="O RVM em 4 passos"
          subtitle="O RVM é um crédito de troca: você não ganha dinheiro, ganha a confiança de poder trocar quando precisar."
        />
        <div className="bg-charcoal rounded-2xl border border-smoke p-6 sm:p-8">
          <ol className="space-y-4">
            {COMO_FUNCIONA.map((step, i) => (
              <li key={i} className="flex items-start gap-3">
                <span className="shrink-0 inline-flex items-center justify-center w-8 h-8 rounded-full bg-esmeralda/15 text-esmeralda text-sm font-bold">
                  {i + 1}
                </span>
                <span className="flex items-center gap-2 text-cream/90">
                  <span aria-hidden className="text-lg">
                    {step.icon}
                  </span>
                  {step.text}
                </span>
              </li>
            ))}
          </ol>
        </div>
      </section>

      {/* De onde vem o valor + taxa BRL */}
      <section className="mb-12 sm:mb-16">
        <SectionHeading
          kicker="De onde vem o valor"
          title="Quem define o preço é a comunidade"
        />
        <div className="grid gap-4 sm:gap-6 lg:grid-cols-3">
          <article className="bg-charcoal rounded-2xl border border-smoke p-6 lg:col-span-2">
            <p className="text-cream/90 leading-relaxed">
              O valor é definido pela própria comunidade —{" "}
              <strong className="text-cream">quem anuncia define o preço</strong>, e a plataforma
              sugere um valor justo baseado em todos os anúncios da categoria. Ninguém impõe
              tabela: o preço justo surge do uso.
            </p>
          </article>

          <div className="bg-charcoal rounded-2xl border border-smoke p-6 flex flex-col justify-center">
            <h3 className="text-sm font-semibold text-silver mb-2">Quanto vale 1 RVM hoje</h3>
            {rateLoading ? (
              <div className="h-9 w-32 bg-smoke rounded animate-pulse" />
            ) : rate ? (
              <div className="flex items-baseline gap-1">
                <span className="text-2xl font-bold text-amber">{brlFmt(rate)}</span>
                <span className="text-silver mx-1 text-sm">/ RVM</span>
              </div>
            ) : (
              <span className="text-sm text-silver">Estimativa indisponível.</span>
            )}
            {disclaimer && rate && (
              <span className="mt-2 text-xs text-silver/80">{disclaimer}</span>
            )}
          </div>
        </div>
      </section>

      {/* Taxa + Demurrage */}
      <section className="mb-12 sm:mb-16">
        <div className="grid gap-4 sm:gap-6 sm:grid-cols-2">
          <article className="bg-charcoal rounded-2xl border border-smoke p-6">
            <div className="inline-flex items-center justify-center w-12 h-12 rounded-xl bg-amber/15 text-2xl mb-4">
              🧰
            </div>
            <h3 className="text-lg font-bold text-amber mb-2">Taxa de 2%</h3>
            <p className="text-sm text-silver leading-relaxed">
              A cada troca, uma pequena taxa de 2% mantém a plataforma no ar. Sem fins
              lucrativos — não há donos nem lucros a distribuir.
            </p>
          </article>

          <article className="bg-charcoal rounded-2xl border border-smoke p-6">
            <div className="inline-flex items-center justify-center w-12 h-12 rounded-xl bg-terracota/15 text-2xl mb-4">
              ♻️
            </div>
            <h3 className="text-lg font-bold text-terracota mb-2">Crédito que circula</h3>
            <p className="text-sm text-silver leading-relaxed">
              Créditos parados por muito tempo perdem um pouquinho de valor — isso faz o dinheiro
              circular em vez de acumular. Há uma isenção para saldos pequenos.
            </p>
          </article>
        </div>
      </section>

      {/* Referências de preço por categoria — dado real (api.pricing) */}
      <section className="mb-12 sm:mb-16">
        <SectionHeading
          kicker="Preço justo"
          title="Referências de preço por categoria"
          subtitle="Preços reais que a comunidade está pedindo agora, com uma sugestão de valor justo. Atualizado a partir dos anúncios ativos."
        />

        {refsError && (
          <div className="bg-charcoal border border-smoke text-silver rounded-xl p-4 text-sm">
            Referências de preço indisponíveis no momento. Tente novamente mais tarde.
          </div>
        )}

        {refs === null && !refsError ? (
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {Array.from({ length: 6 }).map((_, i) => (
              <div
                key={i}
                className="bg-charcoal rounded-2xl border border-smoke p-5 space-y-3"
              >
                <div className="h-4 w-24 bg-smoke rounded animate-pulse" />
                <div className="h-8 w-32 bg-smoke rounded animate-pulse" />
                <div className="h-3 w-full bg-smoke rounded animate-pulse" />
              </div>
            ))}
          </div>
        ) : refs && refs.length === 0 ? (
          <div className="bg-charcoal rounded-2xl border border-smoke p-10 text-center">
            <p className="text-cream font-medium">Ainda não há referências de preço.</p>
            <p className="mt-1 text-sm text-silver">
              Assim que a comunidade começar a anunciar, os preços de referência aparecem aqui.
            </p>
          </div>
        ) : (
          refs && (
            <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
              {refs.map((r) => {
                const medianBrl = brlEstimate(r.RvmMedian, rate ?? r.BrlRate ?? 0);
                const fairBrl = brlEstimate(r.FairSuggestionRvm, rate ?? r.BrlRate ?? 0);
                return (
                  <article
                    key={r.CategoriaId}
                    className="bg-charcoal rounded-2xl border border-smoke p-5 flex flex-col"
                  >
                    <div className="flex items-start justify-between gap-2 mb-3">
                      <h3 className="font-semibold text-cream leading-tight">{catName(r)}</h3>
                      <span className="shrink-0 text-[11px] text-silver whitespace-nowrap">
                        {r.SampleCount} amostra{r.SampleCount === 1 ? "" : "s"}
                      </span>
                    </div>

                    <div className="space-y-2">
                      <PriceRow label="Mediana" rvm={rvmFmt(r.RvmMedian)} brl={medianBrl} />
                      <PriceRow
                        label="Sugestão justa"
                        rvm={rvmFmt(r.FairSuggestionRvm)}
                        brl={fairBrl}
                        highlight
                      />
                    </div>

                    <div className="mt-auto pt-3 border-t border-smoke text-[11px] text-silver">
                      atualizado {formatDate(r.UpdatedAt)}
                    </div>
                  </article>
                );
              })}
            </div>
          )
        )}
      </section>

      {/* CTA rodapé */}
      <section>
        <div className="relative overflow-hidden rounded-3xl border border-smoke text-center px-6 py-12 sm:py-14">
          <div className="absolute inset-0 bg-brand opacity-15" aria-hidden />
          <div className="relative">
            <h2 className="text-2xl sm:text-3xl font-bold text-cream">
              Participe da economia circular
            </h2>
            <p className="mt-3 text-silver max-w-xl mx-auto">
              Troque, doe e cuide com seus vizinhos. Sem fins lucrativos, com moeda social.
            </p>
            <Link
              to={ctaTo}
              className="mt-7 inline-block bg-brand text-ink font-semibold px-7 py-3.5 rounded-xl hover:opacity-90 transition shadow-lg shadow-black/30"
            >
              {user ? "Ir para o feed" : "Participe"}
            </Link>
          </div>
        </div>
      </section>
    </div>
  );
}

function SectionHeading({
  kicker,
  title,
  subtitle,
}: {
  kicker: string;
  title: string;
  subtitle?: string;
}) {
  return (
    <div className="mb-6 sm:mb-8 max-w-3xl">
      <span className="text-xs font-semibold uppercase tracking-wider text-esmeralda">
        {kicker}
      </span>
      <h2 className="mt-1 text-2xl sm:text-3xl lg:text-4xl font-bold text-cream">{title}</h2>
      {subtitle && <p className="mt-2 text-silver">{subtitle}</p>}
    </div>
  );
}

function PriceRow({
  label,
  rvm,
  brl,
  highlight,
}: {
  label: string;
  rvm: string;
  brl: string | null;
  highlight?: boolean;
}) {
  return (
    <div className="flex items-center justify-between">
      <span className="text-sm text-silver">{label}</span>
      <span className="text-right">
        <span className={`rms block ${highlight ? "text-esmeralda" : "text-cream"}`}>RM$ {rvm}</span>
        {brl && <span className="block text-xs text-silver">≈ {brl}</span>}
      </span>
    </div>
  );
}
