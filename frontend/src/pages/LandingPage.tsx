import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Hero } from "../components/Hero";
import { ListingCard } from "../components/ListingCard";
import { api } from "../api/client";
import type { FeedItem } from "../api/types";

const howItWorks = [
  {
    emoji: "🔄",
    title: "Trocar",
    text: "Troque produtos e serviços com a moeda da comunidade RVM.",
    color: "text-esmeralda",
  },
  {
    emoji: "🎁",
    title: "Doar",
    text: "Dá nova vida ao que você não usa mais — doe a quem precisa.",
    color: "text-terracota",
  },
  {
    emoji: "🤝",
    title: "Voluntariar",
    text: "Ofereça seu tempo e receba créditos de ajuda mútua.",
    color: "text-lima",
  },
];

export function LandingPage() {
  const [items, setItems] = useState<FeedItem[] | null>(null);
  const [offline, setOffline] = useState(false);

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

  return (
    <>
      <Hero />

      <section className="mx-auto max-w-6xl px-4 py-16">
        <h2 className="text-2xl font-bold text-cream mb-6">Como funciona</h2>
        <div className="grid gap-4 sm:grid-cols-3">
          {howItWorks.map((h) => (
            <div key={h.title} className="bg-charcoal rounded-xl border border-smoke p-6">
              <div className="text-4xl mb-3" aria-hidden>
                {h.emoji}
              </div>
              <h3 className={`font-semibold text-lg mb-1 ${h.color}`}>{h.title}</h3>
              <p className="text-sm text-silver">{h.text}</p>
            </div>
          ))}
        </div>
      </section>

      <section className="mx-auto max-w-6xl px-4 pb-20">
        <div className="flex items-end justify-between mb-6">
          <h2 className="text-2xl font-bold text-cream">No feed agora</h2>
          <Link to="/feed" className="text-sm text-esmeralda hover:underline">
            Ver tudo →
          </Link>
        </div>
        {offline && (
          <div className="bg-smoke border border-smoke text-silver rounded-xl p-4 text-sm mb-6">
            Não foi possível carregar o feed — backend offline? Rode o servidor .NET na porta 8000.
          </div>
        )}
        <div className="grid gap-4 grid-cols-2 sm:grid-cols-3 lg:grid-cols-6">
          {items === null ? (
            Array.from({ length: 6 }).map((_, i) => (
              <div key={i} className="aspect-[3/4] bg-smoke rounded-xl animate-pulse" />
            ))
          ) : items.length === 0 ? (
            <p className="text-silver col-span-full">
              Ainda não há anúncios. {offline ? "" : "Seja o primeiro!"}
            </p>
          ) : (
            items.slice(0, 6).map((it, i) => <ListingCard key={it.Id ?? i} item={it} />)
          )}
        </div>
      </section>
    </>
  );
}
