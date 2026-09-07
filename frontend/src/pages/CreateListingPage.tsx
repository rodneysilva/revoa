import { useEffect, useRef, useState, type ChangeEvent, type FormEvent } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { ApiError, api } from "../api/client";
import { KIND_LABELS, MODO_META, ALL_MODOS, modosForKind } from "../lib/config";
import { useAuth } from "../auth/AuthContext";
import type {
  Category,
  CreateListingBody,
  Kind,
  Mode,
  MyCommunity,
} from "../api/types";

const KINDS: Kind[] = ["Product", "Service"];

// Limites espelhados do backend (MediaController): máx. 6 fotos por anúncio, 5 MB cada.
const MAX_FOTOS = 6;
const MAX_BYTES = 5 * 1024 * 1024;

const inputCls =
  "mt-1 w-full bg-smoke text-cream rounded-lg border border-smoke focus:border-esmeralda px-4 py-2.5 outline-none";
const labelCls = "block";
const labelTxtCls = "text-sm text-silver";

export function CreateListingPage() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const [categories, setCategories] = useState<Category[]>([]);
  const [loading, setLoading] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const fileRef = useRef<HTMLInputElement>(null);

  const [kind, setKind] = useState<Kind>("Product");
  const [modo, setModo] = useState<Mode>("Trade");
  const [titulo, setTitulo] = useState("");
  const [descricao, setDescricao] = useState("");
  const [imagens, setImagens] = useState<string[]>([]);
  const [preco, setPreco] = useState("0");
  const [condition, setCondition] = useState("Seminovo");
  const [stock, setStock] = useState("1");
  const [unitType, setUnitType] = useState("PerService");
  const [duration, setDuration] = useState("");
  const [voucherDays, setVoucherDays] = useState("30");
  const [cep, setCep] = useState("");
  const [bairro, setBairro] = useState("");
  const [cidade, setCidade] = useState("");
  const [lat, setLat] = useState("");
  const [lng, setLng] = useState("");
  const [categoriaId, setCategoriaId] = useState("");
  // Escopo do anúncio: "" = toda a Revoa (Global); senão o Id da comunidade.
  // escopoPublico: true = feito na comunidade MAS público (Visibility Both —
  // aparece na comunidade e no feed da rede); false = só membros (Community).
  const [minhasComunidades, setMinhasComunidades] = useState<MyCommunity[]>([]);
  const [escopo, setEscopo] = useState("");
  const [escopoPublico, setEscopoPublico] = useState(true);

  // ?community=<id> (atalho "➕ Anunciar algo" da comunidade): pré-seleciona
  // o escopo quando o usuário participa dela.
  const [searchParams] = useSearchParams();
  useEffect(() => {
    const c = searchParams.get("community");
    if (c && minhasComunidades.some((m) => m.Community.Id === c)) {
      setEscopo(c);
    }
  }, [minhasComunidades, searchParams]);

  useEffect(() => {
    api
      .categories()
      .then((c) => {
        if (c.length) {
          setCategories(c);
          setCategoriaId(c[0].Id);
        }
      })
      .catch(() => {
        /* categorias opcionais p/ UX */
      });
  }, []);

  // Garante modo válido ao trocar o tipo.
  useEffect(() => {
    const valid = modosForKind(kind);
    if (!valid.includes(modo)) setModo(valid[0]);
  }, [kind, modo]);

  // Escopo: só oferece comunidades com vínculo (mesma regra validada no backend).
  useEffect(() => {
    if (!user) return;
    api
      .myCommunities()
      .then(setMinhasComunidades)
      .catch(() => {
        /* sem vínculos → anúncio global */
      });
  }, [user]);

  const isFree = modo === "Donate" || modo === "Volunteer";

  // Upload real: cada arquivo vai p/ /api/media (MinIO) e o que entra no anúncio
  // é a URL retornada — o preview já carrega do servidor, igual à exibição final.
  async function onFiles(e: ChangeEvent<HTMLInputElement>) {
    const files = Array.from(e.target.files ?? []);
    e.target.value = ""; // permite reselecionar o mesmo arquivo
    if (files.length === 0) return;

    setError(null);
    setUploading(true);
    try {
      const vagas = MAX_FOTOS - imagens.length;
      for (const f of files.slice(0, Math.max(vagas, 0))) {
        if (f.size > MAX_BYTES) {
          setError(`${f.name} está acima de 5 MB.`);
          continue;
        }
        try {
          const url = await api.uploadImage(f);
          setImagens((prev) => [...prev, url]);
        } catch (err) {
          setError(err instanceof ApiError ? err.message : `Falha ao enviar ${f.name}.`);
        }
      }
    } finally {
      setUploading(false);
    }
  }

  async function submit(e: FormEvent) {
    e.preventDefault();
    setError(null);

    if (!titulo.trim()) return setError("Informe um título.");
    if (!descricao.trim()) return setError("Informe uma descrição.");
    if (!categoriaId) return setError("Escolha uma categoria.");
    if (!modosForKind(kind).includes(modo))
      return setError("Combinação modo/tipo inválida.");

    const precoNum = isFree ? 0 : Number(preco);
    if (!isFree && (isNaN(precoNum) || precoNum < 0)) return setError("Preço inválido.");

    const body: CreateListingBody = {
      Kind: kind,
      Mode: modo,
      Title: titulo.trim(),
      Description: descricao.trim(),
      Imagens: imagens,
      PriceRvm: precoNum,
      Visibility: !escopo ? "Global" : escopoPublico ? "Both" : "Community",
      CommunityId: escopo || undefined,
      CategoryId: categoriaId,
      Neighborhood: bairro.trim() || undefined,
      City: cidade.trim() || undefined,
      PostalCode: cep.trim() || undefined,
      Lat: lat ? Number(lat) : undefined,
      Lng: lng ? Number(lng) : undefined,
    };
    if (kind === "Product") {
      body.Condition = condition;
      const s = Number(stock);
      body.Stock = isNaN(s) ? undefined : s;
    } else {
      body.UnitType = unitType;
      if (duration) body.Duration = Number(duration);
      if (voucherDays) body.VoucherExpiryDays = Number(voucherDays);
    }

    setLoading(true);
    try {
      const id = await api.createListing(body);
      if (id) navigate(`/listings/${id}`);
    } catch (e) {
      setError(e instanceof ApiError ? e.message : "Falha ao criar anúncio.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="app-container">
      <div className="app-read-lg">
      <h1 className="text-2xl font-bold text-cream mb-6">Anunciar</h1>

      {/* Tipo (Kind) */}
      <fieldset className="mb-6">
        <legend className={`${labelTxtCls} mb-2`}>O que você oferece?</legend>
        <div className="flex gap-2">
          {KINDS.map((k) => (
            <button
              key={k}
              type="button"
              onClick={() => setKind(k)}
              className={`flex-1 px-4 py-3 rounded-xl border font-semibold transition ${
                kind === k
                  ? "bg-brand text-ink border-transparent"
                  : "bg-charcoal text-silver border-smoke hover:text-cream"
              }`}
            >
              {KIND_LABELS[k]}
            </button>
          ))}
        </div>
      </fieldset>

      {/* Modo */}
      <fieldset className="mb-6">
        <legend className={`${labelTxtCls} mb-2`}>Modo</legend>
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-2">
          {ALL_MODOS.map((m) => {
            const enabled = modosForKind(kind).includes(m);
            const meta = MODO_META[m];
            return (
              <button
                key={m}
                type="button"
                disabled={!enabled}
                onClick={() => setModo(m)}
                className={`px-3 py-3 rounded-xl border text-sm font-medium transition ${
                  modo === m
                    ? "bg-brand text-ink border-transparent"
                    : enabled
                    ? "bg-charcoal text-cream border-smoke hover:border-esmeralda"
                    : "bg-charcoal text-silver/40 border-smoke cursor-not-allowed"
                }`}
                title={meta.desc}
              >
                <span aria-hidden className="block text-lg">
                  {meta.emoji}
                </span>
                {meta.label}
              </button>
            );
          })}
        </div>
        <p className="mt-2 text-xs text-silver">{MODO_META[modo].desc}</p>
      </fieldset>

      <form onSubmit={submit} className="space-y-4">
        <label className={labelCls}>
          <span className={labelTxtCls}>Título</span>
          <input
            value={titulo}
            onChange={(e) => setTitulo(e.target.value)}
            className={inputCls}
            placeholder="Ex.: Bicicleta infantil seminova"
          />
        </label>

        <label className={labelCls}>
          <span className={labelTxtCls}>Descrição</span>
          <textarea
            value={descricao}
            onChange={(e) => setDescricao(e.target.value)}
            className={inputCls}
            rows={4}
            placeholder="Detalhe o estado, dimensões, combinas, etc."
          />
        </label>

        {/* Fotos (upload real → /api/media; preview já carrega da URL servida) */}
        <div>
          <span className={labelTxtCls}>Fotos ({imagens.length}/{MAX_FOTOS})</span>
          <input
            ref={fileRef}
            type="file"
            accept="image/jpeg,image/png,image/webp,image/gif"
            multiple
            hidden
            onChange={onFiles}
          />
          <button
            type="button"
            onClick={() => fileRef.current?.click()}
            disabled={imagens.length >= MAX_FOTOS || uploading}
            className="mt-1 w-full bg-charcoal text-cream border border-dashed border-smoke rounded-lg px-4 py-3 hover:border-esmeralda transition disabled:opacity-50"
          >
            {uploading ? "Enviando…" : "📷 Adicionar fotos"}
          </button>
          {imagens.length > 0 && (
            <ul className="mt-2 grid grid-cols-3 gap-2">
              {imagens.map((url, i) => (
                <li key={url} className="relative">
                  <img
                    src={url}
                    alt={`Foto ${i + 1}`}
                    className="w-full h-20 object-cover rounded-lg border border-smoke"
                  />
                  <button
                    type="button"
                    onClick={() => setImagens((prev) => prev.filter((_, j) => j !== i))}
                    className="absolute -top-2 -right-2 bg-ink text-cream rounded-full w-6 h-6 text-xs border border-smoke hover:text-rosa"
                    aria-label="Remover foto"
                  >
                    ✕
                  </button>
                </li>
              ))}
            </ul>
          )}
          <p className="mt-1 text-xs text-silver">JPEG, PNG, WebP ou GIF · até 5 MB cada.</p>
        </div>

        {/* Preço */}
        <label className={labelCls}>
          <span className={labelTxtCls}>
            Preço (RVM) {isFree && <span className="text-lima">· doação/voluntariado = Grátis</span>}
          </span>
          <div className="mt-1 flex items-center gap-2">
            <span className="rms text-silver">RM$</span>
            <input
              type="number"
              min={0}
              step="0.01"
              disabled={isFree}
              value={isFree ? "0" : preco}
              onChange={(e) => setPreco(e.target.value)}
              className={inputCls}
            />
          </div>
        </label>

        {/* Campos dinâmicos por Kind */}
        {kind === "Product" ? (
          <div className="grid grid-cols-2 gap-3">
            <label className={labelCls}>
              <span className={labelTxtCls}>Condição</span>
              <select
                value={condition}
                onChange={(e) => setCondition(e.target.value)}
                className={inputCls}
              >
                <option value="Novo">Novo</option>
                <option value="Seminovo">Seminovo</option>
                <option value="Usado">Usado</option>
              </select>
            </label>
            <label className={labelCls}>
              <span className={labelTxtCls}>Estoque</span>
              <input
                type="number"
                min={0}
                value={stock}
                onChange={(e) => setStock(e.target.value)}
                className={inputCls}
              />
            </label>
          </div>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
            <label className={labelCls}>
              <span className={labelTxtCls}>Unidade</span>
              <select
                value={unitType}
                onChange={(e) => setUnitType(e.target.value)}
                className={inputCls}
              >
                <option value="PerService">Por serviço</option>
                <option value="Hours">Por hora</option>
              </select>
            </label>
            <label className={labelCls}>
              <span className={labelTxtCls}>Duração</span>
              <input
                type="number"
                min={0}
                value={duration}
                onChange={(e) => setDuration(e.target.value)}
                className={inputCls}
                placeholder="min"
              />
            </label>
            <label className={labelCls}>
              <span className={labelTxtCls}>Voucher (dias)</span>
              <input
                type="number"
                min={0}
                value={voucherDays}
                onChange={(e) => setVoucherDays(e.target.value)}
                className={inputCls}
              />
            </label>
          </div>
        )}

        {/* Categoria */}
        <label className={labelCls}>
          <span className={labelTxtCls}>Categoria</span>
          <select
            value={categoriaId}
            onChange={(e) => setCategoriaId(e.target.value)}
            className={inputCls}
          >
            {categories.length === 0 && <option value="">(carregando…)</option>}
            {categories.map((c) => (
              <option key={c.Id} value={c.Id}>
                {c.Name}
              </option>
            ))}
          </select>
        </label>

        {/* Escopo — onde o anúncio aparece (só quem participa de comunidades vê) */}
        {minhasComunidades.length > 0 && (
          <div>
            <label className={labelCls}>
              <span className={labelTxtCls}>Onde aparece</span>
              <select
                value={escopo}
                onChange={(e) => setEscopo(e.target.value)}
                className={inputCls}
              >
                <option value="">Toda a Revoa (feed geral)</option>
                {minhasComunidades.map((m) => (
                  <option key={m.Community.Id} value={m.Community.Id}>
                    Em {m.Community.Name}
                  </option>
                ))}
              </select>
            </label>

            {/* Feito na comunidade: público (comunidade + rede) ou só membros */}
            {escopo && (
              <div className="mt-2 grid grid-cols-1 sm:grid-cols-2 gap-2">
                <button
                  type="button"
                  onClick={() => setEscopoPublico(true)}
                  className={`px-4 py-2.5 rounded-xl border text-sm font-medium transition ${
                    escopoPublico
                      ? "bg-brand text-ink border-transparent"
                      : "bg-charcoal text-silver border-smoke hover:text-cream"
                  }`}
                >
                  🌐 Público — comunidade e feed da rede
                </button>
                <button
                  type="button"
                  onClick={() => setEscopoPublico(false)}
                  className={`px-4 py-2.5 rounded-xl border text-sm font-medium transition ${
                    !escopoPublico
                      ? "bg-brand text-ink border-transparent"
                      : "bg-charcoal text-silver border-smoke hover:text-cream"
                  }`}
                >
                  🔒 Só membros da comunidade
                </button>
              </div>
            )}
          </div>
        )}

        {/* Localização (opcional) */}
        <details className="bg-charcoal rounded-xl border border-smoke p-4">
          <summary className={`${labelTxtCls} cursor-pointer`}>
            Localização (opcional)
          </summary>
          <div className="mt-3 grid grid-cols-2 gap-3">
            <label className={labelCls}>
              <span className={labelTxtCls}>CEP</span>
              <input value={cep} onChange={(e) => setCep(e.target.value)} className={inputCls} />
            </label>
            <label className={labelCls}>
              <span className={labelTxtCls}>Bairro</span>
              <input value={bairro} onChange={(e) => setBairro(e.target.value)} className={inputCls} />
            </label>
            <label className={labelCls}>
              <span className={labelTxtCls}>Cidade</span>
              <input value={cidade} onChange={(e) => setCidade(e.target.value)} className={inputCls} />
            </label>
            <label className={labelCls}>
              <span className={labelTxtCls}>Latitude</span>
              <input value={lat} onChange={(e) => setLat(e.target.value)} className={inputCls} />
            </label>
            <label className={labelCls}>
              <span className={labelTxtCls}>Longitude</span>
              <input value={lng} onChange={(e) => setLng(e.target.value)} className={inputCls} />
            </label>
          </div>
        </details>

        {error && <p className="text-rosa text-sm">{error}</p>}

        <div className="flex items-center gap-3">
          <button
            disabled={loading || uploading}
            className="flex-1 bg-brand text-ink font-semibold px-6 py-3 rounded-xl disabled:opacity-60"
          >
            {loading ? "Publicando…" : "Publicar anúncio"}
          </button>
        </div>

        {user && (
          <p className="text-xs text-silver text-center">
            Você publica como <span className="text-cream">{user.nome || user.email}</span>.
          </p>
        )}
      </form>
      </div>
    </div>
  );
}
