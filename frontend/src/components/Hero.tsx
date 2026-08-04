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

  return (
    <section className="relative overflow-hidden border-b border-smoke">
      <div className="absolute inset-0 bg-brand opacity-[0.12]" aria-hidden />
      <div className="absolute inset-0 bg-help opacity-[0.08]" aria-hidden />
      <div
        className="absolute -top-24 -left-16 w-72 h-72 rounded-full bg-esmeralda/20 blur-3xl"
        aria-hidden
      />
      <div
        className="absolute -bottom-24 -right-16 w-80 h-80 rounded-full bg-sky/20 blur-3xl"
        aria-hidden
      />

      <div className="app-bar-inner relative py-16 sm:py-24 lg:py-28">
        <div className="app-read text-center">
          <h1 className="wordmark text-5xl sm:text-6xl lg:text-7xl xl:text-8xl tracking-tight">
            revoa.me
          </h1>
          <p
            className={`mt-6 text-2xl sm:text-3xl lg:text-4xl font-semibold text-cream transition-opacity duration-300 ${
              fade ? "opacity-100" : "opacity-0"
            }`}
            aria-live="polite"
          >
            {SLOGANS[idx]}
          </p>
          <p className="mt-5 text-base sm:text-lg text-silver">
            Economia circular e ajuda mútua, sem fins lucrativos, com a moeda
            social RVM. Você oferece o que sabe fazer ou o que não usa mais —
            troca, doa e cuida, de vizinho para vizinho.
          </p>
          <div className="mt-9 flex flex-col sm:flex-row gap-3 justify-center">
            <Link
              to={primaryTo}
              className="bg-brand text-ink font-semibold px-7 py-3.5 rounded-xl hover:opacity-90 transition shadow-lg shadow-black/30"
            >
              Ofereça o que você tem
            </Link>
            <Link
              to="/feed"
              className="border border-smoke bg-charcoal/40 text-cream font-semibold px-7 py-3.5 rounded-xl hover:border-esmeralda transition"
            >
              Explorar o feed
            </Link>
          </div>
        </div>
      </div>
    </section>
  );
}
