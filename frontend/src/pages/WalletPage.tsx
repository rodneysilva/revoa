import { Link } from "react-router-dom";

export function WalletPage() {
  return (
    <div className="mx-auto max-w-2xl px-4 py-16">
      <h1 className="text-2xl font-bold text-cream mb-6">Carteira RVM</h1>
      <div className="bg-charcoal rounded-2xl border border-smoke p-8 text-center">
        <div className="text-5xl mb-3" aria-hidden>
          🪪
        </div>
        <p className="rms text-3xl text-cream mb-1">RM$ —</p>
        <p className="text-silver text-sm mb-6">
          Sua carteira (Safe invisível, self-custody) estará disponível em breve.
        </p>
        <Link to="/feed" className="bg-brand text-ink font-semibold px-5 py-2.5 rounded-xl">
          Ver o feed
        </Link>
      </div>
    </div>
  );
}
