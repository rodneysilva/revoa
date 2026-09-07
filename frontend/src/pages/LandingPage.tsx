import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import {
  Check,
  Eye,
  Hand,
  Heart,
  HeartHandshake,
  PartyPopper,
  Recycle,
  Sprout,
  SquarePen,
  Star,
  Wrench,
  type LucideIcon,
} from "lucide-react";
import { Hero } from "../components/Hero";
import { ListingCard } from "../components/ListingCard";
import { api } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import type { FeedItem, Mode } from "../api/types";

import { MODO_META } from "../lib/config";

interface Guide {
  modo: Mode;
  title: string;
  intro: string;
  chip: string;
  ringActive: string;
  steps: { icon: LucideIcon; text: string }[];
  faqs: { q: string; a: string }[];
}

const MODO_CARDS: {
  modo: Mode;
  title: string;
  desc: string;
  accent: string;
  iconBg: string;
  hover: string;
}[] = [
  {
    modo: "Trade",
    title: "Trocar",
    desc: "Troque produtos e serviços com o RVM, a moeda da comunidade.",
    accent: "text-esmeralda",
    iconBg: "bg-esmeralda/15",
    hover: "hover:border-esmeralda/60",
  },
  {
    modo: "Resell",
    title: "Repassar",
    desc: "Repassa por um valor baixo em RVM, acessível para mais gente.",
    accent: "text-rosa",
    iconBg: "bg-rosa/15",
    hover: "hover:border-rosa/60",
  },
  {
    modo: "Donate",
    title: "Doar",
    desc: "Dê nova vida ao que não usa — de graça, a quem precisa.",
    accent: "text-terracota",
    iconBg: "bg-terracota/15",
    hover: "hover:border-terracota/60",
  },
  {
    modo: "Volunteer",
    title: "Voluntariar",
    desc: "Ofereça seu tempo e habilidades. A comunidade agradece.",
    accent: "text-lima",
    iconBg: "bg-lima/15",
    hover: "hover:border-lima/60",
  },
];

const GUIDES: Guide[] = [
  {
    modo: "Trade",
    title: "Trocar",
    intro:
      "Troque produtos e serviços com o RVM, a moeda da comunidade. Você oferece, recebe RVM e usa quando precisa.",
    chip: "bg-esmeralda/15 text-esmeralda",
    ringActive: "ring-esmeralda/60",
    steps: [
      { icon: SquarePen, text: "Você anuncia o que oferece (produto ou serviço) com um preço justo em RVM." },
      { icon: Eye, text: "Quem precisa encontra seu anúncio no feed e oferece os RVM." },
      { icon: HeartHandshake, text: "Vocês combinam a entrega do produto ou a prestação do serviço." },
      { icon: Check, text: "Na confirmação, você recebe seus RVM (menos uma pequena taxa que sustenta a plataforma — sem lucro)." },
      { icon: Star, text: "Vocês se avaliam e a confiança da comunidade cresce." },
    ],
    faqs: [
      { q: "O que é RVM?", a: "É o crédito de troca da comunidade: você ganha oferecendo e usa quando precisa. Não é dinheiro e não dá lucro." },
      { q: "Preciso pagar em dinheiro?", a: "Não. As trocas acontecem em RVM, a moeda da comunidade." },
      { q: "E se algo der errado?", a: "Há uma janela de 72h para resolver; um árbitro da comunidade ajuda em caso de disputa." },
    ],
  },
  {
    modo: "Resell",
    title: "Repassar",
    intro:
      "Repassa algo por um valor baixo em RVM, para que mais gente possa acessar. Acessibilidade no centro.",
    chip: "bg-rosa/15 text-rosa",
    ringActive: "ring-rosa/60",
    steps: [
      { icon: SquarePen, text: "Você anuncia um produto por um preço baixo em RVM — acessível para mais gente." },
      { icon: Eye, text: "Quem precisa encontra e oferece os RVM." },
      { icon: HeartHandshake, text: "Vocês combinam a entrega no bairro." },
      { icon: Check, text: "Na confirmação, você recebe — e o item segue circulando." },
      { icon: Star, text: "Vocês se avaliam." },
    ],
    faqs: [
      { q: "O que é repassar?", a: "É repassar algo por um valor baixo em RVM, para ser acessível a mais pessoas." },
      { q: "É diferente de doar?", a: "Sim: no repassar você recebe um RVM baixo; na doação, é de graça (RM$ 0)." },
    ],
  },
  {
    modo: "Donate",
    title: "Doar",
    intro:
      "Dê nova vida ao que não usa mais — de graça. Você escolhe quem recebe e ganha o reconhecimento da comunidade.",
    chip: "bg-terracota/15 text-terracota",
    ringActive: "ring-terracota/60",
    steps: [
      { icon: SquarePen, text: "Você anuncia o que não usa mais, de graça (RM$ 0)." },
      { icon: Hand, text: "Quem precisa pede na fila de interesse." },
      { icon: Heart, text: "Você escolhe quem vai receber (por proximidade, mensagem e reputação)." },
      { icon: HeartHandshake, text: "Vocês combinam a entrega." },
      { icon: PartyPopper, text: "Pronto: encontrou um novo lar — e você ganha reconhecimento (selo de doador + pontos de ajuda)." },
    ],
    faqs: [
      { q: "Preciso pagar algo?", a: "Não, doar é de graça. Você não paga nem recebe RVM na doação." },
      { q: "É seguro?", a: "Você escolhe quem recebe e combina a entrega no seu bairro." },
      { q: "Ganho algo doando?", a: "Reconhecimento da comunidade: selo de doador e pontos de ajuda. Sem lucro, sem valorização." },
    ],
  },
  {
    modo: "Volunteer",
    title: "Voluntariar",
    intro:
      "Ofereça uma ajuda que você sabe prestar — de graça. A comunidade agradece e você ganha reconhecimento.",
    chip: "bg-lima/15 text-lima",
    ringActive: "ring-lima/60",
    steps: [
      { icon: SquarePen, text: "Você anuncia uma ajuda que sabe prestar, de graça (RM$ 0)." },
      { icon: Hand, text: "Quem precisa pede." },
      { icon: Heart, text: "Você escolhe quem ajudar." },
      { icon: HeartHandshake, text: "Vocês combinam e você presta a ajuda." },
      { icon: PartyPopper, text: "A comunidade agradece — você ganha selo de voluntário + pontos de ajuda." },
    ],
    faqs: [
      { q: "Preciso pagar algo?", a: "Não, voluntariar é de graça." },
      { q: "Que tipo de ajuda posso oferecer?", a: "Qualquer serviço: conserto, aula, transporte, marcenaria… o que você sabe fazer." },
      { q: "Ganho algo?", a: "Reconhecimento da comunidade: selo de voluntário e pontos de ajuda." },
    ],
  },
];

const PILARES: { icon: LucideIcon; title: string; text: string }[] = [
  { icon: Recycle, title: "Tudo é compartilhado e transformável", text: "Serviço, produto e RVM se convertem. Nada fica parado — tudo circula e muda de forma." },
  { icon: Sprout, title: "Reuso com propósito", text: "O que você não usa mais encontra um novo lar onde é útil, em vez de ir para o lixo." },
  { icon: Wrench, title: "Habilidade = poder de troca", text: "Seu serviço gera RVM, que vira outros produtos ou serviços quando você precisa." },
  { icon: Heart, title: "Ajuda mútua em primeiro lugar", text: "Doar e voluntariar valem tanto quanto trocar. A economia é o meio — cuidar é o fim." },
];

export function LandingPage() {
  const { user } = useAuth();
  const [items, setItems] = useState<FeedItem[] | null>(null);
  const [offline, setOffline] = useState(false);
  const [activeModo, setActiveModo] = useState<Mode | null>(null);

  useEffect(() => {
    let active = true;
    api
      .feed({ page: 1 })
      .then((data) => {
        if (active) setItems(data);
      })
      .catch(() => {
        if (active) {
          setOffline(true);
          setItems([]);
        }
      });
    return () => {
      active = false;
    };
  }, []);

  function focusModo(m: Mode) {
    setActiveModo(m);
    requestAnimationFrame(() => {
      document
        .getElementById(`guia-${m}`)
        ?.scrollIntoView({ behavior: "smooth", block: "start" });
    });
  }

  const ctaTo = user?.verified ? "/listings/new" : "/register";

  return (
    <>
      <Hero />

      {/* FEED em destaque — prova social imediata logo após a hero. */}
      <section id="feed" className="scroll-mt-20">
        <div className="app-container">
          <div className="flex flex-col sm:flex-row sm:items-end gap-4 mb-8">
            <div>
              <h2 className="text-2xl sm:text-3xl lg:text-4xl font-bold text-cream">
                O que a comunidade está oferecendo
              </h2>
              <p className="mt-2 text-silver max-w-2xl">
                Anúncios reais de quem já faz parte do revoa.me — troque, doe e
                cuide com seus vizinhos.
              </p>
            </div>
            <Link
              to="/listings"
              className="sm:ml-auto inline-flex items-center gap-1 text-sm font-semibold text-esmeralda hover:underline whitespace-nowrap"
            >
              Ver tudo →
            </Link>
          </div>

          {offline && (
            <div className="bg-smoke border border-smoke text-silver rounded-xl p-4 text-sm mb-6">
              Não foi possível carregar o feed — backend offline? Rode o servidor
              .NET na porta 8000.
            </div>
          )}

          {items === null ? (
            <div className="grid gap-4 grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 2xl:grid-cols-6">
              {Array.from({ length: 8 }).map((_, i) => (
                <div
                  key={i}
                  className="flex flex-col bg-charcoal rounded-xl border border-smoke overflow-hidden"
                >
                  <div className="aspect-[4/5] bg-smoke animate-pulse" />
                  <div className="p-4 space-y-2">
                    <div className="h-3 w-16 bg-smoke rounded animate-pulse" />
                    <div className="h-4 w-full bg-smoke rounded animate-pulse" />
                    <div className="h-3 w-20 bg-smoke rounded animate-pulse" />
                  </div>
                </div>
              ))}
            </div>
          ) : items.length === 0 ? (
            <div className="bg-charcoal rounded-2xl border border-smoke p-10 text-center">
              <p className="text-cream font-medium">Ainda não há anúncios por aqui.</p>
              <p className="mt-1 text-sm text-silver">
                {offline
                  ? "Volte em breve — a comunidade está chegando."
                  : "Seja o primeiro a oferecer algo!"}
              </p>
              {!offline && (
                <Link
                  to={ctaTo}
                  className="mt-5 inline-block bg-brand text-ink font-semibold px-5 py-2.5 rounded-xl"
                >
                  Ofereça o que você tem
                </Link>
              )}
            </div>
          ) : (
            <div className="grid gap-4 grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 2xl:grid-cols-6">
              {items.slice(0, 8).map((it, i) => (
                <ListingCard key={it.Id ?? i} item={it} />
              ))}
            </div>
          )}
        </div>
      </section>

      <section>
        <div className="app-container">
          <div className="text-center mb-8">
            <h2 className="text-2xl sm:text-3xl lg:text-4xl font-bold text-cream">
              Como você quer participar?
            </h2>
            <p className="mt-2 text-silver max-w-xl mx-auto">
              Escolha um modo e veja o passo a passo — simples, para todas as idades.
            </p>
          </div>
          <div className="grid gap-4 sm:gap-6 sm:grid-cols-2 lg:grid-cols-4">
            {MODO_CARDS.map((c) => {
              const MIcon = MODO_META[c.modo].icon;
              return (
              <button
                key={c.modo}
                type="button"
                onClick={() => focusModo(c.modo)}
                aria-label={`Ver como funciona: ${c.title}`}
                className={`group text-left bg-charcoal rounded-2xl border border-smoke p-6 transition ${c.hover}`}
              >
                <div
                  className={`inline-flex items-center justify-center w-14 h-14 rounded-xl mb-4 ${c.iconBg} ${c.accent}`}
                >
                  <MIcon aria-hidden className="w-7 h-7" />
                </div>
                <h3 className={`text-xl font-bold mb-1 ${c.accent}`}>{c.title}</h3>
                <p className="text-sm text-silver">{c.desc}</p>
                <span
                  className={`mt-4 inline-block text-sm font-semibold ${c.accent} group-hover:underline`}
                >
                  Ver como funciona →
                </span>
              </button>
              );
            })}
          </div>
        </div>
      </section>

      <section id="como-funciona" className="border-t border-smoke">
        <div className="app-container">
          <div className="text-center mb-10">
            <h2 className="text-2xl sm:text-3xl lg:text-4xl font-bold text-cream">
              Como funciona
            </h2>
            <p className="mt-2 text-silver max-w-2xl mx-auto">
              O passo a passo de cada modo, do anúncio até o novo lar. Sem complicação.
            </p>
          </div>

          <div className="space-y-6">
            {GUIDES.map((g) => {
              const active = activeModo === g.modo;
              return (
                <div
                  key={g.modo}
                  id={`guia-${g.modo}`}
                  className={`scroll-mt-24 bg-charcoal/60 rounded-2xl border p-6 sm:p-8 ring-2 transition ${
                    active ? `${g.ringActive} border-transparent` : "ring-transparent border-smoke"
                  }`}
                >
                  <div className="flex items-center gap-3 mb-3">
                    {(() => {
                      const GIcon = MODO_META[g.modo].icon;
                      return <GIcon aria-hidden className="w-7 h-7" />;
                    })()}
                    <span
                      className={`inline-flex items-center gap-1 rounded-full px-3 py-1 text-sm font-semibold ${g.chip}`}
                    >
                      {g.title}
                    </span>
                  </div>
                  <p className="text-cream/90 mb-6">{g.intro}</p>

                  <ol className="space-y-3">
                    {g.steps.map((st, i) => {
                      const SIcon = st.icon;
                      return (
                      <li key={i} className="flex gap-3 items-start">
                        <span
                          className="shrink-0 inline-flex items-center justify-center w-8 h-8 rounded-full bg-smoke text-sm font-bold text-cream"
                          aria-hidden
                        >
                          {i + 1}
                        </span>
                        <span className="flex items-center gap-2 text-cream/90">
                          <SIcon aria-hidden className="w-4 h-4 shrink-0" />
                          <span>{st.text}</span>
                        </span>
                      </li>
                      );
                    })}
                  </ol>

                  <div className="mt-6 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
                    {g.faqs.map((f, i) => (
                      <details
                        key={i}
                        className="bg-smoke rounded-xl border border-smoke px-4 py-3"
                      >
                        <summary className="cursor-pointer list-none flex items-center justify-between gap-3 text-cream font-medium text-sm">
                          <span>{f.q}</span>
                          <span className="faq-plus text-silver transition-transform" aria-hidden>
                            +
                          </span>
                        </summary>
                        <p className="mt-2 text-sm text-silver">{f.a}</p>
                      </details>
                    ))}
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      </section>

      <section className="border-t border-smoke">
        <div className="app-container">
          <div className="text-center mb-8">
            <h2 className="text-2xl sm:text-3xl lg:text-4xl font-bold text-cream">
              Por que o revoa.me
            </h2>
            <p className="mt-2 text-silver max-w-2xl mx-auto">
              Uma comunidade que se ajuda, sem fins lucrativos.
            </p>
          </div>
          <div className="grid gap-4 sm:gap-6 sm:grid-cols-2 lg:grid-cols-4">
            {PILARES.map((p) => {
              const PIcon = p.icon;
              return (
              <div key={p.title} className="bg-charcoal rounded-2xl border border-smoke p-6">
                <div className="mb-3 text-esmeralda" aria-hidden>
                  <PIcon className="w-7 h-7" />
                </div>
                <h3 className="font-semibold text-cream mb-1">{p.title}</h3>
                <p className="text-sm text-silver">{p.text}</p>
              </div>
              );
            })}
          </div>
        </div>
      </section>

      <section className="border-t border-smoke">
        <div className="app-container">
          <div className="relative overflow-hidden rounded-3xl border border-smoke text-center px-6 py-12 sm:py-16">
            <div className="absolute inset-0 bg-community opacity-20" aria-hidden />
            <div className="relative">
              <h2 className="text-2xl sm:text-3xl lg:text-4xl font-bold text-cream">
                Comunidade que troca, doa e cuida.
              </h2>
              <p className="mt-3 text-silver max-w-xl mx-auto">
                Junte-se a quem acredita que o que não usa merece um novo lar.
              </p>
              <Link
                to={ctaTo}
                className="mt-7 inline-block bg-brand text-ink font-semibold px-7 py-3.5 rounded-xl hover:opacity-90 transition shadow-lg shadow-black/30"
              >
                {user ? "Ofereça o que você tem" : "Participe da comunidade"}
              </Link>
            </div>
          </div>
        </div>
      </section>
    </>
  );
}
