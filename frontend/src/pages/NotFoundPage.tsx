import { Link } from "react-router-dom";

export function NotFoundPage() {
  return (
    <div className="mx-auto max-w-md px-4 py-24 text-center">
      <div className="text-6xl mb-4" aria-hidden>
        🧭
      </div>
      <h1 className="text-3xl font-bold text-cream mb-2">Página não encontrada</h1>
      <p className="text-silver mb-6">
        Talvez este link já tenha encontrado um novo lar.
      </p>
      <Link to="/" className="bg-brand text-ink font-semibold px-6 py-3 rounded-xl">
        Voltar ao início
      </Link>
    </div>
  );
}
