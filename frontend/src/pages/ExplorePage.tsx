import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { ListingCard, ListingCardSkeleton } from "../components/ListingCard";
import { ApiError, api } from "../api/client";
import type { Category, FeedItem, FeedParams, Kind, Mode } from "../api/types";
import { useAuth } from "../auth/AuthContext";

const PAGE_SIZE = 24;

type KindFilter = "Todos" | Kind;
type ModoFilter = "Todos" | Mode;
type SortKey = "recente" | "preco-asc" | "preco-desc";

const KIND_OPTIONS: KindFilter[] = ["Todos", "Product", "Service"];
const KIND_LABEL: Record<KindFilter, string> = {
  Todos: "Todos",
  Product: "Produtos",
  Service: "Serviços",
};
const MODO_OPTIONS: ModoFilter[] = ["Todos", "Trade", "Resell", "Donate", "Volunteer"];
const SORT_OPTIONS: { value: SortKey; label: string }[] = [
  { value: "recente", label: "Mais recentes" },
  { value: "preco-asc", label: "Preço ↑" },
  { value: "preco-desc", label: "Preço ↓" },
];

const inputCls =
  "w-full bg-smoke text-cream rounded-lg border border-smoke focus:border-esmeralda px-3 py-2 outline-none text-sm";
const labelCls = "block text-xs font-medium text-silver mb-1.5";

// Ícone por categoria (Baymard: categorias com thumbnail na exploração).
// Match por nome/slug — categorias novas caem no fallback ♻️.
const CATEGORY_EMOJI: [RegExp, string][] = [
  [/mobili|sofá|sofa|cadeira|mesa|estante|cama/i, "🪑"],
  [/roupa|vestu|moda|tênis|tenis|sapato/i, "👕"],
  [/livro|leitura|revista/i, "📚"],
  [/eletr|celular|notebook|tv|áudio|audio|fone/i, "🔌"],
  [/ferrament|obra|constru/i, "🔧"],
  [/brinquedo|jogo/i, "🧸"],
  [/esporte|bicicleta|bike|fit|academia/i, "⚽"],
  [/comida|aliment|horta|comida|cozinha/i, "🍲"],
  [/jardin|planta|flores/i, "🪴"],
  [/músic|musica|instrument|aula|curso|idioma|ensino/i, "🎓"],
  [/bebê|bebe|criança|crianca|infantil/i, "🍼"],
  [/pet|animal|cachorro|gato/i, "🐾"],
  [/saúde|saude|bem-estar|beleza|cabelo|estética|estetica/i, "💊"],
  [/reforma|reparo|manuten|elétric|eletric|encanador|pintura/i, "🔨"],
  [/tecnologia|inform|comput|program|design/i, "💻"],
  [/doação|doacao|caridade|ajuda/i, "🎁"],
];

function categoryEmoji(c: Category): string {
  const s = `${c.Name} ${c.Slug ?? ""}`;
  for (const [rx, emoji] of CATEGORY_EMOJI) {
    if (rx.test(s)) return emoji;
  }
  return "♻️";
}

export function ExplorePage() {
  const { user } = useAuth();

  const [q, setQ] = useState("");
  const [debouncedQ, setDebouncedQ] = useState("");
  const [kind, setKind] = useState<KindFilter>("Todos");
  const [modo, setModo] = useState<ModoFilter>("Todos");
  const [categoriaId, setCategoriaId] = useState("");
  const [precoMin, setPrecoMin] = useState("");
  const [precoMax, setPrecoMax] = useState("");
  const [doarApenas, setDoarApenas] = useState(false);
  const [sort, setSort] = useState<SortKey>("recente");

  // Proximidade (hiperlocal, UF-06): coords lembradas entre visitas; o raio é
  // sempre escolha explícita do usuário.
  const [geo, setGeo] = useState<{ lat: number; lng: number } | null>(() => {
    try {
      const raw = localStorage.getItem("revoa.geo");
      return raw ? (JSON.parse(raw) as { lat: number; lng: number }) : null;
    } catch {
      return null;
    }
  });
  const [raio, setRaio] = useState<number | null>(null);
  const [geoErro, setGeoErro] = useState<string | null>(null);

  function pedirLocalizacao() {
    setGeoErro(null);
    if (!navigator.geolocation) {
      setGeoErro("Seu navegador não suporta geolocalização.");
      return;
    }
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        const g = { lat: pos.coords.latitude, lng: pos.coords.longitude };
        localStorage.setItem("revoa.geo", JSON.stringify(g));
        setGeo(g);
        setRaio((r) => r ?? 10);
      },
      () =>
        setGeoErro(
          "Não foi possível obter sua localização — verifique a permissão do navegador."
        ),
      { timeout: 10_000 }
    );
  }

  function limparGeo() {
    localStorage.removeItem("revoa.geo");
    setGeo(null);
    setRaio(null);
  }

  const [categories, setCategories] = useState<Category[]>([]);
  const [items, setItems] = useState<FeedItem[]>([]);
  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(true);
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [filtersOpen, setFiltersOpen] = useState(false);

  useEffect(() => {
    api.categories().then(setCategories).catch(() => {});
  }, []);

  useEffect(() => {
    const t = setTimeout(() => setDebouncedQ(q.trim()), 350);
    return () => clearTimeout(t);
  }, [q]);

  const buildParams = useMemo(
    () =>
      (p: number): FeedParams => ({
        page: p,
        q: debouncedQ || undefined,
        kind: kind === "Todos" ? undefined : kind,
        mode: modo === "Todos" ? undefined : modo,
        categoryId: categoriaId || undefined,
        priceMin: precoMin ? Number(precoMin) : undefined,
        priceMax: precoMax ? Number(precoMax) : undefined,
        donationOnly: doarApenas || undefined,
        sort,
        radius: geo && raio ? raio : undefined,
        lat: geo && raio ? geo.lat : undefined,
        lng: geo && raio ? geo.lng : undefined,
      }),
    [debouncedQ, kind, modo, categoriaId, precoMin, precoMax, doarApenas, sort, geo, raio]
  );

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError(null);
    api
      .feed(buildParams(1))
      .then((data) => {
        if (!active) return;
        setItems(data);
        setPage(1);
        setHasMore(data.length >= PAGE_SIZE);
      })
      .catch((e) => {
        if (active)
          setError(e instanceof ApiError ? e.message : "Erro ao buscar anúncios.");
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, [buildParams]);

  async function loadMore() {
    if (loadingMore || !hasMore) return;
    setLoadingMore(true);
    const next = page + 1;
    try {
      const data = await api.feed(buildParams(next));
      setItems((prev) => [...prev, ...data]);
      setPage(next);
      setHasMore(data.length >= PAGE_SIZE);
    } catch (e) {
      setError(e instanceof ApiError ? e.message : "Erro ao carregar mais.");
    } finally {
      setLoadingMore(false);
    }
  }

  function clearFilters() {
    setQ("");
    setDebouncedQ("");
    setKind("Todos");
    setModo("Todos");
    setCategoriaId("");
    setPrecoMin("");
    setPrecoMax("");
    setDoarApenas(false);
    setSort("recente");
    limparGeo();
  }

  const activeCount =
    (kind !== "Todos" ? 1 : 0) +
    (modo !== "Todos" ? 1 : 0) +
    (categoriaId ? 1 : 0) +
    (precoMin || precoMax ? 1 : 0) +
    (doarApenas ? 1 : 0) +
    (sort !== "recente" ? 1 : 0) +
    (debouncedQ ? 1 : 0) +
    (geo && raio ? 1 : 0);

  const Filters = (
    <div className="bg-charcoal rounded-xl border border-smoke p-4 space-y-4">
      <div>
        <label className="block">
          <span className="sr-only">Buscar</span>
          <input
            type="search"
            value={q}
            onChange={(e) => setQ(e.target.value)}
            placeholder="Buscar por título ou descrição…"
            className={inputCls}
          />
        </label>
      </div>

      <div>
        <span className={labelCls}>Tipo</span>
        <div className="flex gap-1 bg-smoke rounded-lg border border-smoke p-1">
          {KIND_OPTIONS.map((k) => (
            <button
              key={k}
              onClick={() => setKind(k)}
              className={`flex-1 px-2 py-1.5 rounded-md text-sm font-medium transition ${
                kind === k ? "bg-brand text-ink" : "text-silver hover:text-cream"
              }`}
            >
              {KIND_LABEL[k]}
            </button>
          ))}
        </div>
      </div>

      <div>
        <span className={labelCls}>Modo</span>
        <div className="flex flex-wrap gap-1 bg-smoke rounded-lg border border-smoke p-1">
          {MODO_OPTIONS.map((m) => (
            <button
              key={m}
              onClick={() => setModo(m)}
              className={`flex-1 min-w-[5.5rem] px-2 py-1.5 rounded-md text-sm font-medium transition ${
                modo === m ? "bg-brand text-ink" : "text-silver hover:text-cream"
              }`}
            >
              {m === "Todos" ? "Todos" : m}
            </button>
          ))}
        </div>
      </div>

      <div>
        <label className="block">
          <span className={labelCls}>Categoria</span>
          <select
            value={categoriaId}
            onChange={(e) => setCategoriaId(e.target.value)}
            className={inputCls}
          >
            <option value="">Todas as categorias</option>
            {categories.map((c) => (
              <option key={c.Id} value={c.Id}>
                {c.Name}
              </option>
            ))}
          </select>
        </label>
      </div>

      <div>
        <span className={labelCls}>Faixa de preço (RM$)</span>
        <div className="flex items-center gap-2">
          <input
            type="number"
            min={0}
            inputMode="numeric"
            value={precoMin}
            onChange={(e) => setPrecoMin(e.target.value)}
            placeholder="mín"
            className={inputCls}
            aria-label="Preço mínimo"
          />
          <span className="text-silver">—</span>
          <input
            type="number"
            min={0}
            inputMode="numeric"
            value={precoMax}
            onChange={(e) => setPrecoMax(e.target.value)}
            placeholder="máx"
            className={inputCls}
            aria-label="Preço máximo"
          />
        </div>
      </div>

      <label className="flex items-center gap-2 cursor-pointer select-none">
        <input
          type="checkbox"
          checked={doarApenas}
          onChange={(e) => setDoarApenas(e.target.checked)}
          className="w-4 h-4 accent-esmeralda"
        />
        <span className="text-sm text-cream">Só grátis (doações e voluntariado)</span>
      </label>

      <div>
        <label className="block">
          <span className={labelCls}>Ordenar por</span>
          <select
            value={sort}
            onChange={(e) => setSort(e.target.value as SortKey)}
            className={inputCls}
          >
            {SORT_OPTIONS.map((s) => (
              <option key={s.value} value={s.value}>
                {s.label}
              </option>
            ))}
          </select>
        </label>
      </div>

      <div className="pt-1 border-t border-smoke">
        <span className="block text-xs font-medium text-silver pt-3 mb-1.5">
          📍 Proximidade
        </span>
        {!geo ? (
          <button
            onClick={pedirLocalizacao}
            className="w-full text-left text-sm text-esmeralda hover:underline"
          >
            Usar minha localização
          </button>
        ) : (
          <div className="space-y-2">
            <select
              value={raio ?? ""}
              onChange={(e) => setRaio(e.target.value ? Number(e.target.value) : null)}
              className={inputCls}
              aria-label="Raio de busca"
            >
              <option value="">Sem raio (tudo)</option>
              <option value="2">2 km</option>
              <option value="5">5 km</option>
              <option value="10">10 km</option>
              <option value="25">25 km</option>
              <option value="50">50 km</option>
            </select>
            <button
              onClick={limparGeo}
              className="text-xs text-silver hover:text-cream"
            >
              Esquecer localização
            </button>
          </div>
        )}
        {geoErro && <p className="text-xs text-rosa mt-1">{geoErro}</p>}
      </div>

      {activeCount > 0 && (
        <button
          onClick={clearFilters}
          className="w-full text-sm text-esmeralda hover:underline pt-1"
        >
          Limpar filtros ({activeCount})
        </button>
      )}
    </div>
  );

  return (
    <div className="app-container">
      <div className="flex flex-col sm:flex-row sm:items-center gap-3 mb-4">
        <h1 className="text-2xl sm:text-3xl font-bold text-cream">Explorar</h1>
        <span className="text-sm text-silver sm:ml-1">
          {loading
            ? "Buscando…"
            : `${items.length} anúncio${items.length === 1 ? "" : "s"}${
                geo && raio ? ` · 📍 até ${raio} km` : ""
              }`}
        </span>
        {user?.verified && (
          <Link
            to="/listings/new"
            className="sm:ml-auto bg-brand text-ink font-semibold px-4 py-2 rounded-lg text-sm text-center"
          >
            + Anunciar
          </Link>
        )}
      </div>

      {/* Categorias em destaque (Baymard: subcategorias com ícone no topo da
          exploração; clicar na ativa limpa) */}
      {categories.length > 0 && (
        <div className="flex gap-3 overflow-x-auto pb-3 mb-4">
          <button
            onClick={() => setCategoriaId("")}
            className={`shrink-0 w-24 h-24 rounded-xl border flex flex-col items-center justify-center gap-1.5 transition ${
              !categoriaId
                ? "border-esmeralda bg-esmeralda/10"
                : "border-smoke bg-charcoal hover:border-esmeralda/60"
            }`}
          >
            <span className="text-2xl" aria-hidden>
              ♻️
            </span>
            <span className="text-xs font-medium text-cream px-1">Tudo</span>
          </button>
          {categories.map((c) => {
            const active = categoriaId === c.Id;
            return (
              <button
                key={c.Id}
                onClick={() => setCategoriaId(active ? "" : c.Id)}
                title={c.Description || c.Name}
                className={`shrink-0 w-24 h-24 rounded-xl border flex flex-col items-center justify-center gap-1.5 transition ${
                  active
                    ? "border-esmeralda bg-esmeralda/10"
                    : "border-smoke bg-charcoal hover:border-esmeralda/60"
                }`}
              >
                <span className="text-2xl" aria-hidden>
                  {categoryEmoji(c)}
                </span>
                <span className="text-xs font-medium text-cream leading-tight line-clamp-2 px-1.5 text-center">
                  {c.Name}
                </span>
              </button>
            );
          })}
        </div>
      )}

      <div className="grid gap-6 lg:grid-cols-[280px_1fr]">
        <aside className="hidden lg:block">
          <div className="sticky top-20">{Filters}</div>
        </aside>

        <div>
          <button
            onClick={() => setFiltersOpen((o) => !o)}
            className="lg:hidden w-full mb-4 flex items-center justify-between bg-charcoal rounded-lg border border-smoke px-4 py-2.5 text-sm font-medium text-cream"
          >
            <span>Filtros{activeCount > 0 ? ` (${activeCount})` : ""}</span>
            <span aria-hidden>{filtersOpen ? "✕" : "☰"}</span>
          </button>
          {filtersOpen && <div className="lg:hidden mb-6">{Filters}</div>}

          {error && (
            <div className="bg-smoke border border-smoke text-silver rounded-xl p-4 text-sm mb-6">
              {error}
            </div>
          )}

          {loading ? (
            <div className="grid gap-4 grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 2xl:grid-cols-5 3xl:grid-cols-6">
              {Array.from({ length: 10 }).map((_, i) => (
                <ListingCardSkeleton key={i} />
              ))}
            </div>
          ) : items.length === 0 ? (
            <div className="bg-charcoal rounded-xl border border-smoke p-8 text-center">
              <p className="text-silver mb-1">Nenhum anúncio com esses filtros.</p>
              <p className="text-sm text-silver/70 mb-4">
                Tente ajustar a busca ou limpar os filtros.
              </p>
              {activeCount > 0 && (
                <button
                  onClick={clearFilters}
                  className="inline-block bg-brand text-ink font-semibold px-5 py-2.5 rounded-xl"
                >
                  Limpar filtros
                </button>
              )}
              {!activeCount && user?.verified && (
                <Link
                  to="/listings/new"
                  className="inline-block bg-brand text-ink font-semibold px-5 py-2.5 rounded-xl"
                >
                  Criar o primeiro anúncio
                </Link>
              )}
            </div>
          ) : (
            <>
              <div className="grid gap-4 grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 2xl:grid-cols-5 3xl:grid-cols-6">
                {items.map((it) => (
                  <ListingCard key={it.Id} item={it} />
                ))}
              </div>
              {hasMore && (
                <div className="text-center mt-8">
                  <button
                    onClick={loadMore}
                    disabled={loadingMore}
                    className="border border-smoke text-cream font-semibold px-6 py-2.5 rounded-xl hover:border-esmeralda disabled:opacity-60"
                  >
                    {loadingMore ? "Carregando…" : "Carregar mais"}
                  </button>
                </div>
              )}
            </>
          )}
        </div>
      </div>
    </div>
  );
}
