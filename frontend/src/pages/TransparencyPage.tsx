import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { api } from "../api/client";
import type { Category, PriceReference } from "../api/types";
import { useAuth } from "../auth/AuthContext";
import { brlEstimate } from "../lib/format";
import { useBrlRate } from "../lib/useBrlRate";

interface TokenomiaCard {
  icon: string;
  title: string;
  desc: string;
  accent: string;
  iconBg: string;
  ring: string;
}

const TOKENOMIA: TokenomiaCard[] = [
  {
    icon: "🪙",
    title: "RVM — crédito de troca",
    desc:
      "O RVM é a moeda social da comunidade: você recebe oferecendo produtos ou serviços e usa quando precisa. " +
      "Não é ativo financeiro nem criptomoeda para investir — é crédito de troca, em auto-custódia on-chain.",
    accent: "text-esmeralda",
    iconBg: "bg-esmeralda/15",
    ring: "hover:border-esmeralda/50",
  },
  {
    icon: "🚰",
    title: "Faucet de boas-vindas",
    desc:
      "Ao se cadastrar e verificar, você recebe o equivalente a R$20 em RVM para começar a trocar. " +
      "Quem tiver um cupom on-chain (opcional) recebe um bônus extra. Um por dispositivo — sem incentivo a contas múltiplas.",
    accent: "text-sky",
    iconBg: "bg-sky/15",
    ring: "hover:border-sky/50",
  },
  {
    icon: "🧰",
    title: "Taxa de 2% → Fundo Comunitário",
    desc:
      "Toda troca paga uma pequena taxa de 2% que vai integralmente para o Fundo Comunitário, que custeia a infraestrutura. " +
      "Sem fins lucrativos: não há acionistas nem distribuição de lucros. Transparente e auditável.",
    accent: "text-amber",
    iconBg: "bg-amber/15",
    ring: "hover:border-amber/50",
  },
  {
    icon: "♻️",
    title: "Demurrage — o crédito que circula",
    desc:
      "Saldos parados perdem 0,5% ao mês, mas só sobre o que excede R$100 equivalentes (piso de isenção). " +
      "Não é lucro para ninguém: o RVM queimado é retirado de circulação. Incentiva a usar, não a acumular.",
    accent: "text-terracota",
    iconBg: "bg-terracota/15",
    ring: "hover:border-terracota/50",
  },
];

interface Principio {
  icon: string;
  title: string;
  text: string;
  accent: string;
}

const PRINCIPIOS: Principio[] = [
  {
    icon: "🤲",
    title: "Sem fins lucrativos",
    text: "Sem acionistas, sem distribuição de lucros. Tudo que entra custeia a plataforma e a comunidade.",
    accent: "text-esmeralda",
  },
  {
    icon: "🔐",
    title: "Auto-custódia (carteira invisível)",
    text: "Seus RVM são seus, on-chain. Você não entrega as chaves para ninguém — a carteira fica escondida atrás do seu acesso.",
    accent: "text-sky",
  },
  {
    icon: "🚪",
    title: "Aberto para navegar, fechado para agir",
    text: "Qualquer pessoa pode ver tudo. Para anunciar, trocar ou interagir, é preciso se cadastrar e verificar.",
    accent: "text-amber",
  },
  {
    icon: "❤️",
    title: "Ajuda mútua em primeiro lugar",
    text: "Doar e voluntariar valem tanto quanto trocar. A economia é o meio — cuidar das pessoas é o fim.",
    accent: "text-terracota",
  },
  {
    icon: "⭐",
    title: "Reputação on-chain",
    text: "Avaliações e reconhecimento (doador, voluntário) ficam registrados e constroem confiança na comunidade.",
    accent: "text-lima",
  },
  {
    icon: "📍",
    title: "Hiperlocalidade",
    text: "Trocas, doações e ajuda acontecem no bairro, entre vizinhos. O próximo vizinho é mais perto do que parece.",
    accent: "text-rosa",
  },
];

interface Fase {
  tag: string;
  titulo: string;
  desc: string;
}

const ROADMAP: Fase[] = [
  {
    tag: "Fase 0",
    titulo: "Fundação",
    desc: "Identidade, contratos, infraestrutura e arquitetura modular definidas.",
  },
  {
    tag: "Fase 1–2",
    titulo: "Catálogo & comunidades",
    desc: "Anúncios (produto/serviço), troca com escrow, comunidades e chat.",
  },
  {
    tag: "Fase 3",
    titulo: "Inteligência de preços",
    desc: "Mediana comunitária + IPCA/IBGE + Ollama: preço justo emergente do uso.",
  },
  {
    tag: "Fase 4",
    titulo: "Transparência & impacto",
    desc: "Esta página. Próximo: painel público de impacto e prestação de contas.",
  },
];

const SOURCE_LABEL: Record<string, string> = {
  community: "Comunidade",
  ipca: "IPCA / IBGE",
  ollama: "Ollama Qwen 7B",
};

const SOURCE_CHIP: Record<string, string> = {
  community: "bg-esmeralda/15 text-esmeralda",
  ipca: "bg-sky/15 text-sky",
  ollama: "bg-roxo/15 text-roxo",
};

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

function formatIpcMonth(yyyymm?: string): string {
  if (!yyyymm || yyyymm.length < 6) return yyyymm ?? "—";
  const y = yyyymm.slice(0, 4);
  const m = yyyymm.slice(4, 6);
  return `${m}/${y}`;
}

function parseSources(raw?: string): string[] {
  if (!raw) return [];
  return raw
    .split(",")
    .map((s) => s.trim().toLowerCase())
    .filter(Boolean);
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
          <span className="wordmark">revoa.me</span> — economia circular e de ajuda mútua, sem fins
          lucrativos. Aqui ficam públicas as regras, os parâmetros e os preços de referência do
          projeto.
        </p>
      </header>

      {/* Tokenomia */}
      <section className="mb-12 sm:mb-16">
        <SectionHeading
          kicker="Tokenomia"
          title="Como o RVM funciona"
          subtitle="Os parâmetros públicos do projeto. O RVM é crédito de troca — não dá lucro."
        />
        <div className="grid gap-4 sm:gap-6 sm:grid-cols-2">
          {TOKENOMIA.map((c) => (
            <article
              key={c.title}
              className={`bg-charcoal rounded-2xl border border-smoke p-6 transition ${c.ring}`}
            >
              <div
                className={`inline-flex items-center justify-center w-12 h-12 rounded-xl text-2xl mb-4 ${c.iconBg}`}
              >
                <span aria-hidden>{c.icon}</span>
              </div>
              <h3 className={`text-lg font-bold mb-2 ${c.accent}`}>{c.title}</h3>
              <p className="text-sm text-silver leading-relaxed">{c.desc}</p>
            </article>
          ))}
        </div>

        {/* Estimativa BRL — dado real (api.brlRate) */}
        <div className="mt-4 sm:mt-6 bg-charcoal rounded-2xl border border-smoke p-6 sm:p-8">
          <div className="flex flex-col sm:flex-row sm:items-center gap-4 sm:gap-8">
            <div className="sm:max-w-xs">
              <h3 className="text-lg font-bold text-cream mb-1">Estimativa em BRL</h3>
              <p className="text-sm text-silver leading-relaxed">
                Símbolica: dá noção de valor nas trocas, alinhada à inflação.{" "}
                <strong className="text-cream">Não é câmbio nem preço real</strong> — o RVM não é
                ativo financeiro.
              </p>
            </div>
            <div className="sm:ml-auto flex flex-col sm:items-end gap-1">
              {rateLoading ? (
                <div className="h-9 w-40 bg-smoke rounded animate-pulse" />
              ) : rate ? (
                <div className="flex items-baseline gap-1">
                  <span className="text-2xl sm:text-3xl font-bold text-cream">1 RVM</span>
                  <span className="text-silver mx-1">≈</span>
                  <span className="text-2xl sm:text-3xl font-bold text-amber">
                    {brlFmt(rate)}
                  </span>
                </div>
              ) : (
                <span className="text-sm text-silver">Estimativa indisponível no momento.</span>
              )}
              {disclaimer && rate && (
                <span className="text-xs text-silver/80 max-w-xs sm:text-right">{disclaimer}</span>
              )}
            </div>
          </div>
        </div>
      </section>

      {/* Referências de preço por categoria — dado real (api.pricing) */}
      <section className="mb-12 sm:mb-16">
        <SectionHeading
          kicker="Preço justo"
          title="Referências de preço por categoria"
          subtitle="A mediana do que a comunidade realmente pede, combinada com IPCA/IBGE e refinada por IA. É um preço justo emergente do uso — não uma tabela imposta."
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
                <div className="h-3 w-2/3 bg-smoke rounded animate-pulse" />
              </div>
            ))}
          </div>
        ) : refs && refs.length === 0 ? (
          <div className="bg-charcoal rounded-2xl border border-smoke p-10 text-center">
            <p className="text-cream font-medium">Ainda não há referências de preço.</p>
            <p className="mt-1 text-sm text-silver">
              Elas surgem da mediana dos anúncios ativos. Assim que a comunidade trocar, os dados
              aparecem aqui.
            </p>
          </div>
        ) : (
          refs && (
            <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
              {refs.map((r) => {
                const sources = parseSources(r.SourcesUsed);
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

                    <div className="space-y-2 mb-4">
                      <PriceRow label="Mediana" rvm={rvmFmt(r.RvmMedian)} brl={medianBrl} />
                      <PriceRow
                        label="Sugestão justa"
                        rvm={rvmFmt(r.FairSuggestionRvm)}
                        brl={fairBrl}
                        highlight
                      />
                      {r.BrlReference != null && (
                        <div className="flex items-center justify-between text-xs text-silver">
                          <span>Referência admin</span>
                          <span className="text-amber">{brlFmt(r.BrlReference)}</span>
                        </div>
                      )}
                    </div>

                    {sources.length > 0 && (
                      <div className="flex flex-wrap gap-1.5 mb-3">
                        {sources.map((s) => (
                          <span
                            key={s}
                            className={`text-[11px] font-medium px-2 py-0.5 rounded-full ${
                              SOURCE_CHIP[s] ?? "bg-smoke text-silver"
                            }`}
                          >
                            {SOURCE_LABEL[s] ?? s}
                          </span>
                        ))}
                      </div>
                    )}

                    <div className="mt-auto pt-3 border-t border-smoke flex items-center justify-between text-[11px] text-silver">
                      {r.LastIpcRate != null ? (
                        <span>
                          IPCA {formatIpcMonth(r.LastIpcMonth)}: {r.LastIpcRate}%
                        </span>
                      ) : (
                        <span>IPCA n/d</span>
                      )}
                      <span>atualizado {formatDate(r.UpdatedAt)}</span>
                    </div>
                  </article>
                );
              })}
            </div>
          )
        )}
        <p className="mt-3 text-xs text-silver/80">
          Mediana comunitária dos anúncios ativos + semente BRL + IPCA IBGE + Ollama. Estimativa
          BRL simbólica — referência de valor, não preço de mercado.
        </p>
      </section>

      {/* Princípios / compromissos */}
      <section className="mb-12 sm:mb-16">
        <SectionHeading
          kicker="Compromissos"
          title="Princípios do projeto"
          subtitle="O que guia cada decisão — e que pode ser cobrado de quem cuida do revoa.me."
        />
        <div className="grid gap-4 sm:gap-6 sm:grid-cols-2 lg:grid-cols-3">
          {PRINCIPIOS.map((p) => (
            <article key={p.title} className="bg-charcoal rounded-2xl border border-smoke p-6">
              <div className="text-3xl mb-3" aria-hidden>
                {p.icon}
              </div>
              <h3 className={`font-semibold mb-1 ${p.accent}`}>{p.title}</h3>
              <p className="text-sm text-silver leading-relaxed">{p.text}</p>
            </article>
          ))}
        </div>
      </section>

      {/* Prestação de contas / roadmap */}
      <section className="mb-12 sm:mb-16">
        <SectionHeading
          kicker="Prestação de contas"
          title="De onde viemos e para onde vamos"
          subtitle="Um projeto em construção. Mostramos o caminho e o que ainda falta — com honestidade."
        />
        <div className="bg-charcoal rounded-2xl border border-smoke p-6 sm:p-8">
          <ol className="relative border-l border-smoke ml-3 space-y-6">
            {ROADMAP.map((f) => (
              <li key={f.tag} className="pl-6">
                <span className="absolute -left-[7px] mt-1.5 w-3 h-3 rounded-full bg-brand" />
                <span className="text-xs font-semibold uppercase tracking-wide text-esmeralda">
                  {f.tag}
                </span>
                <h3 className="text-cream font-semibold mt-0.5">{f.titulo}</h3>
                <p className="text-sm text-silver mt-1 max-w-2xl">{f.desc}</p>
              </li>
            ))}
          </ol>
          <div className="mt-6 rounded-xl bg-smoke/60 border border-smoke px-4 py-3 text-sm text-silver">
            📊 Dados de impacto (trocas, doações, RVM circulando) em breve — painel público é a
            próxima entrega desta fase.
          </div>
        </div>
      </section>

      {/* CTA rodapé */}
      <section>
        <div className="relative overflow-hidden rounded-3xl border border-smoke text-center px-6 py-12 sm:py-14">
          <div className="absolute inset-0 bg-brand opacity-15" aria-hidden />
          <div className="relative">
            <h2 className="text-2xl sm:text-3xl font-bold text-cream">Participe da economia circular</h2>
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

export default TransparencyPage;
