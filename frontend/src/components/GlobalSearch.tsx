import { useEffect, useRef, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { MessagesSquare, Package, Wrench } from "lucide-react";
import { api } from "../api/client";
import type { Mode, SearchResults } from "../api/types";
import { MODO_META } from "../lib/config";

const MIN_CHARS = 2;
const DEBOUNCE_MS = 300;

// Ícone do modo do anúncio (lucide, herdando a cor do texto).
function ModoIcon({ mode }: { mode: Mode }) {
  const Icon = MODO_META[mode].icon;
  return <Icon aria-hidden className="w-3 h-3" />;
}

// Busca global do header: dropdown agrupado (Anúncios + Comunidades) enquanto
// digita; Enter leva ao feed completo com ?q=. Fechável por fora-clique/Esc.
export function GlobalSearch() {
  const [termo, setTermo] = useState("");
  const [open, setOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const [results, setResults] = useState<SearchResults | null>(null);
  const boxRef = useRef<HTMLDivElement | null>(null);
  const navigate = useNavigate();

  // Debounce: só busca quando o termo para de mudar (evita request por tecla).
  useEffect(() => {
    const q = termo.trim();
    if (q.length < MIN_CHARS) {
      setResults(null);
      setLoading(false);
      return;
    }
    setLoading(true);
    let active = true;
    const timer = setTimeout(() => {
      api
        .search(q)
        .then((r) => {
          if (active) setResults(r);
        })
        .catch(() => {
          if (active) setResults({ Listings: [], Communities: [] });
        })
        .finally(() => {
          if (active) setLoading(false);
        });
    }, DEBOUNCE_MS);
    return () => {
      active = false;
      clearTimeout(timer);
    };
  }, [termo]);

  // Fora-clique e Esc fecham o dropdown.
  useEffect(() => {
    function onDocClick(e: MouseEvent) {
      if (boxRef.current && !boxRef.current.contains(e.target as Node))
        setOpen(false);
    }
    function onKey(e: KeyboardEvent) {
      if (e.key === "Escape") setOpen(false);
    }
    document.addEventListener("mousedown", onDocClick);
    document.addEventListener("keydown", onKey);
    return () => {
      document.removeEventListener("mousedown", onDocClick);
      document.removeEventListener("keydown", onKey);
    };
  }, []);

  function submit(ev: React.FormEvent) {
    ev.preventDefault();
    const q = termo.trim();
    if (q.length < MIN_CHARS) return;
    setOpen(false);
    navigate(`/feed?q=${encodeURIComponent(q)}`);
  }

  const q = termo.trim();
  const showResults = open && q.length >= MIN_CHARS;
  const nada =
    !loading &&
    results &&
    results.Listings.length === 0 &&
    results.Communities.length === 0;

  return (
    <div ref={boxRef} className="relative hidden md:block">
      <form onSubmit={submit}>
        <span className="sr-only">Buscar na revoa</span>
        <input
          type="search"
          value={termo}
          onChange={(e) => {
            setTermo(e.target.value);
            setOpen(true);
          }}
          onFocus={() => setOpen(true)}
          placeholder="Buscar anúncios e comunidades…"
          aria-label="Buscar anúncios e comunidades"
          className="w-52 lg:w-64 bg-smoke/60 text-cream rounded-lg border border-smoke focus:border-esmeralda focus:w-64 lg:focus:w-72 px-3.5 py-2 outline-none text-sm placeholder:text-silver/70 transition-all"
        />
      </form>

      {showResults && (
        <div className="absolute left-0 top-full mt-2 w-80 lg:w-96 bg-charcoal border border-smoke rounded-xl shadow-xl overflow-hidden z-50">
          {/* loading || !results: o efeito que seta loading roda DEPOIS do paint —
              sem a guarda de results, o frame pós-2ª-tecla caía aqui com null. */}
          {loading || !results ? (
            <p className="px-4 py-3 text-sm text-silver">Buscando…</p>
          ) : nada ? (
            <p className="px-4 py-3 text-sm text-silver">
              Nada encontrado para “{q}”.
            </p>
          ) : (
            <div className="max-h-[70vh] overflow-y-auto py-1">
              {results!.Listings.length > 0 && (
                <Group label="Anúncios">
                  {results!.Listings.map((l) => (
                    <Link
                      key={l.Id}
                      to={`/listings/${l.Id}`}
                      onClick={() => setOpen(false)}
                      className="flex items-center gap-3 px-3 py-2 hover:bg-smoke/60 transition"
                    >
                      <span className="w-9 h-9 shrink-0 rounded-lg bg-smoke overflow-hidden flex items-center justify-center">
                        {l.PrimeiraImagem ? (
                          <img
                            src={l.PrimeiraImagem}
                            alt=""
                            className="w-full h-full object-cover"
                          />
                        ) : (
                          <span aria-hidden className="text-silver">
                            {l.Kind === "Service" ? (
                              <Wrench className="w-4 h-4" />
                            ) : (
                              <Package className="w-4 h-4" />
                            )}
                          </span>
                        )}
                      </span>
                      <span className="min-w-0">
                        <span className="block text-sm text-cream truncate">
                          {l.Title}
                        </span>
                        <span className="block text-xs text-silver">
                          <ModoIcon mode={l.Mode} />{" "}
                          {l.PriceRvm === 0 ? (
                            "Grátis"
                          ) : (
                            <span className="rms">RM$ {l.PriceRvm.toLocaleString("pt-BR")}</span>
                          )}
                          {l.City ? ` · ${l.City}` : ""}
                        </span>
                      </span>
                    </Link>
                  ))}
                </Group>
              )}

              {results!.Communities.length > 0 && (
                <Group label="Comunidades">
                  {results!.Communities.map((c) => (
                    <Link
                      key={c.Id}
                      to={`/community/${c.Id}`}
                      onClick={() => setOpen(false)}
                      className="flex items-center gap-3 px-3 py-2 hover:bg-smoke/60 transition"
                    >
                      <span className="w-9 h-9 shrink-0 rounded-lg bg-smoke flex items-center justify-center text-silver" aria-hidden>
                        <MessagesSquare className="w-4 h-4" />
                      </span>
                      <span className="min-w-0">
                        <span className="block text-sm text-cream truncate">
                          {c.Name}
                        </span>
                        <span className="block text-xs text-silver">
                          {c.MembersCount} membro{c.MembersCount === 1 ? "" : "s"}
                          {c.City ? ` · ${c.City}` : ""}
                        </span>
                      </span>
                    </Link>
                  ))}
                </Group>
              )}

              <button
                type="button"
                onClick={() => {
                  setOpen(false);
                  navigate(`/feed?q=${encodeURIComponent(q)}`);
                }}
                className="w-full text-left px-4 py-2.5 text-sm text-esmeralda hover:bg-smoke/60 border-t border-smoke"
              >
                Ver todos os resultados →
              </button>
            </div>
          )}
        </div>
      )}
    </div>
  );
}

function Group({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="py-1">
      <p className="px-4 pt-1.5 pb-1 text-[11px] font-bold uppercase tracking-wider text-silver/80">
        {label}
      </p>
      {children}
    </div>
  );
}
