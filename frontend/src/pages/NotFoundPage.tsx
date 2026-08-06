import { Link } from "react-router-dom";

export function NotFoundPage() {
  return (
    <div className="app-container">
      <div className="app-read text-center">
      <div className="text-6xl mb-4" aria-hidden>
        🧭
      </div>
      <h1 className="text-3xl font-bold text-cream mb-2">Página não encontrada</h1>
      <p className="text-silver mb-6">
        Talvez este link já tenha encontrado um novo lar.
      </p>
      <div className="flex items-center justify-center gap-3">
        <Link to="/" className="bg-brand text-ink font-semibold px-6 py-3 rounded-xl">
          Voltar ao início
        </Link>
        <Link
          to="/feed"
          className="border border-smoke text-cream font-semibold px-6 py-3 rounded-xl hover:border-esmeralda"
        >
          Ir ao feed
        </Link>
      </div>
      </div>
    </div>
  );
}
