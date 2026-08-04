import { useEffect, useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { ApiError, api } from "../api/client";
import { KIND_LABELS, MODO_META, modosForKind } from "../lib/config";
import { useAuth } from "../auth/AuthContext";
import type {
  Category,
  CreateListingBody,
  Kind,
  Modo,
  Visibilidade,
} from "../api/types";

const KINDS: Kind[] = ["Product", "Service"];
const ALL_MODOS: Modo[] = ["Trocar", "Repassar", "Doar", "Voluntariar"];
const VISIBILIDADES: Visibilidade[] = ["Global", "Comunidade", "Ambos"];

const inputCls =
  "mt-1 w-full bg-smoke text-cream rounded-lg border border-smoke focus:border-esmeralda px-4 py-2.5 outline-none";
const labelCls = "block";
const labelTxtCls = "text-sm text-silver";

export function CreateListingPage() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const [categories, setCategories] = useState<Category[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [ok, setOk] = useState(false);

  const [kind, setKind] = useState<Kind>("Product");
  const [modo, setModo] = useState<Modo>("Trocar");
  const [titulo, setTitulo] = useState("");
  const [descricao, setDescricao] = useState("");
  const [imagens, setImagens] = useState<string[]>([]);
  const [imgInput, setImgInput] = useState("");
  const [preco, setPreco] = useState("0");
  const [condition, setCondition] = useState("seminovo");
  const [stock, setStock] = useState("1");
  const [unitType, setUnitType] = useState("per-service");
  const [duration, setDuration] = useState("");
  const [voucherDays, setVoucherDays] = useState("30");
  const [cep, setCep] = useState("");
  const [bairro, setBairro] = useState("");
  const [cidade, setCidade] = useState("");
  const [lat, setLat] = useState("");
  const [lng, setLng] = useState("");
  const [categoriaId, setCategoriaId] = useState("");
  const [visibilidade, setVisibilidade] = useState<Visibilidade>("Global");

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

  const isFree = modo === "Doar" || modo === "Voluntariar";

  function addImagem() {
    const url = imgInput.trim();
    if (!url) return;
    setImagens((prev) => [...prev, url]);
    setImgInput("");
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
      Modo: modo,
      Titulo: titulo.trim(),
      Descricao: descricao.trim(),
      Imagens: imagens,
      PrecoRvm: precoNum,
      Visibilidade: visibilidade,
      CategoriaId: categoriaId,
      Bairro: bairro.trim() || undefined,
      Cidade: cidade.trim() || undefined,
      Cep: cep.trim() || undefined,
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
      setOk(true);
      if (id) navigate(`/listings/${id}`);
    } catch (e) {
      setError(e instanceof ApiError ? e.message : "Falha ao criar anúncio.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="mx-auto max-w-2xl px-4 py-8">
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

        {/* Imagens (URLs) */}
        <div>
          <span className={labelTxtCls}>Imagens (URLs)</span>
          <div className="mt-1 flex gap-2">
            <input
              value={imgInput}
              onChange={(e) => setImgInput(e.target.value)}
              className={inputCls}
              placeholder="https://…/foto.jpg"
              onKeyDown={(e) => {
                if (e.key === "Enter") {
                  e.preventDefault();
                  addImagem();
                }
              }}
            />
            <button
              type="button"
              onClick={addImagem}
              className="shrink-0 bg-charcoal text-cream border border-smoke rounded-lg px-4 hover:border-esmeralda"
            >
              +
            </button>
          </div>
          {imagens.length > 0 && (
            <ul className="mt-2 space-y-1">
              {imagens.map((url, i) => (
                <li
                  key={i}
                  className="flex items-center gap-2 bg-smoke rounded-lg px-3 py-1.5 text-sm text-cream"
                >
                  <span className="truncate flex-1">{url}</span>
                  <button
                    type="button"
                    onClick={() => setImagens((prev) => prev.filter((_, j) => j !== i))}
                    className="text-silver hover:text-rosa"
                    aria-label="Remover imagem"
                  >
                    ✕
                  </button>
                </li>
              ))}
            </ul>
          )}
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
                <option value="novo">Novo</option>
                <option value="seminovo">Seminovo</option>
                <option value="usado">Usado</option>
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
                <option value="per-service">Por serviço</option>
                <option value="hours">Por hora</option>
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
                {c.Nome}
              </option>
            ))}
          </select>
        </label>

        {/* Visibilidade */}
        <label className={labelCls}>
          <span className={labelTxtCls}>Visibilidade</span>
          <select
            value={visibilidade}
            onChange={(e) => setVisibilidade(e.target.value as Visibilidade)}
            className={inputCls}
          >
            {VISIBILIDADES.map((v) => (
              <option key={v} value={v}>
                {v}
              </option>
            ))}
          </select>
        </label>

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
            disabled={loading}
            className="flex-1 bg-brand text-ink font-semibold px-6 py-3 rounded-xl disabled:opacity-60"
          >
            {loading ? "Publicando…" : ok ? "Publicar outro" : "Publicar anúncio"}
          </button>
        </div>

        {user && (
          <p className="text-xs text-silver text-center">
            Você publica como <span className="text-cream">{user.nome || user.email}</span>.
          </p>
        )}
      </form>
    </div>
  );
}
