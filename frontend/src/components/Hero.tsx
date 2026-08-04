import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { SLOGANS } from "../lib/config";
import { useAuth } from "../auth/AuthContext";

export function Hero() {
  const { user } = useAuth();
  const [idx, setIdx] = useState(0);
  const [fade, setFade] = useState(true);

  useEffect(() => {
    const id = setInterval(() => {
      setFade(false);
      setTimeout(() => {
        setIdx((i) => (i + 1) % SLOGANS.length);
        setFade(true);
      }, 250);
    }, 3500);
    return () => clearInterval(id);
  }, []);

  const verified = !!user?.verified;
  const primaryTo = verified ? "/listings/new" : "/register";
  const primaryLabel = verified ? "Ofereça o que você tem" : "Participe da comunidade";

  return (
    <section className="relative overflow-hidden">
      <div className="absolute inset-0 bg-brand opacity-10" aria-hidden />
      <div className="relative mx-auto max-w-6xl px-4 py-20 sm:py-28 text-center">
        <h1 className="wordmark text-5xl sm:text-7xl mb-6">revoa.me</h1>
        <p
          className={`text-2xl sm:text-3xl font-semibold text-cream transition-opacity duration-300 ${
            fade ? "opacity-100" : "opacity-0"
          }`}
        >
          {SLOGANS[idx]}
        </p>
        <p className="mt-4 text-silver max-w-2xl mx-auto">
          A moeda da comunidade RVM conecta quem tem o que oferecer a quem precisa —
          troque, doe e cuide, de vizinho para vizinho.
        </p>
        <div className="mt-8 flex flex-col sm:flex-row gap-3 justify-center">
          <Link
            to={primaryTo}
            className="bg-brand text-ink font-semibold px-6 py-3 rounded-xl hover:opacity-90 transition"
          >
            {primaryLabel}
          </Link>
          <Link
            to="/feed"
            className="border border-smoke text-cream font-semibold px-6 py-3 rounded-xl hover:border-esmeralda transition"
          >
            Ver o feed
          </Link>
        </div>
      </div>
    </section>
  );
}
