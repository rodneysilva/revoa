import { useEffect, useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { ApiError, api } from "../api/client";
import { CommunityCard } from "../components/CommunityCard";
import { useAuth } from "../auth/AuthContext";
import {
  EIXO_EMOJI,
  EIXO_FILTERS,
  EIXO_LABEL,
  EIXOS,
  VISIBILIDADES,
  VISIBILIDADE_LABEL,
} from "../lib/community";
import type {
  Community,
  CreateCommunityBody,
  CommunityAxis,
  CommunityVisibility,
  MyCommunity,
} from "../api/types";

type EixoFilter = (typeof EIXO_FILTERS)[number];

const inputCls =
  "mt-1 w-full bg-smoke text-cream rounded-lg border border-smoke focus:border-esmeralda px-4 py-2.5 outline-none";
const labelCls = "block";
const labelTxtCls = "text-sm text-silver";

export function CommunityPage() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [items, setItems] = useState<Community[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filter, setFilter] = useState<EixoFilter>("Todos");
  const [showCreate, setShowCreate] = useState(false);
  const [busca, setBusca] = useState("");

  // "Minhas comunidades" (GET /mine) — só faz sentido para quem está logado.
  const [mine, setMine] = useState<MyCommunity[] | null>(null);

  useEffect(() => {
    if (!user) {
      setMine(null);
      return;
    }
    let active = true;
    api
      .myCommunities()
      .then((m) => {
        if (active) setMine(m);
      })
      .catch(() => {
        if (active) setMine([]);
      });
    return () => {
      active = false;
    };
  }, [user]);

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError(null);
    api
      .communities({ axis: filter === "Todos" ? undefined : filter })
      .then((d) => {
        if (active) setItems(d);
      })
      .catch((e) => {
        if (active)
          setError(e instanceof ApiError ? e.message : "Erro ao carregar comunidades.");
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, [filter]);

  // Busca client-side sobre a lista carregada (a API de comunidades ainda não
  // tem `q` — filtro simples por nome/descrição/local).
  const termo = busca.trim().toLowerCase();
  const visiveis = termo
    ? items.filter((c) =>
        `${c.Name} ${c.Description ?? ""} ${c.Neighborhood ?? ""} ${c.City ?? ""}`
          .toLowerCase()
          .includes(termo)
      )
    : items;

  return (
    <div className="app-container">
      <div className="flex flex-col sm:flex-row sm:items-end gap-3 mb-5">
        <div>
          <h1 className="text-2xl sm:text-3xl font-bold text-cream">Comunidades</h1>
          <p className="text-silver text-sm mt-1 max-w-2xl">
            Grupos de vizinhos por bairro, interesse ou causa. Espaço de troca, doação e
            ajuda mútua.
          </p>
        </div>
        {user?.verified && (
          <button
            onClick={() => setShowCreate(true)}
            className="sm:ml-auto bg-community text-ink font-semibold px-4 py-2 rounded-lg text-sm"
          >
            + Criar comunidade
          </button>
        )}
      </div>

      {/* ══ Minhas comunidades (vínculo do usuário) ══ */}
      {mine !== null && mine.length > 0 && (
        <section className="mb-10">
          <h2 className="text-xs font-bold uppercase tracking-wider text-silver mb-3">
            Minhas comunidades
          </h2>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
            {mine.map((m) => (
              <CommunityCard key={m.Community.Id} c={m.Community} role={m.Role} />
            ))}
          </div>
        </section>
      )}

      {/* ══ Descobrir ══ */}
      <section>
        <div className="flex flex-col sm:flex-row sm:items-center gap-3 mb-4">
          <h2 className="text-lg font-bold text-cream">
            {mine && mine.length > 0 ? "Descobrir comunidades" : "Comunidades"}
          </h2>
          <input
            type="search"
            value={busca}
            onChange={(e) => setBusca(e.target.value)}
            placeholder="Buscar por nome, bairro ou cidade…"
            className="sm:ml-auto sm:max-w-xs w-full bg-smoke text-cream rounded-lg border border-smoke focus:border-esmeralda px-4 py-2 outline-none text-sm"
          />
        </div>

        <div className="flex gap-2 flex-wrap mb-6">
          {EIXO_FILTERS.map((f) => (
            <button
              key={f}
              onClick={() => setFilter(f)}
              className={`px-3 py-1.5 rounded-full text-sm font-medium border transition ${
                filter === f
                  ? "bg-community text-ink border-transparent"
                  : "border-smoke text-silver hover:text-cream"
              }`}
            >
              {f === "Todos" ? "Todos" : `${EIXO_EMOJI[f]} ${EIXO_LABEL[f]}`}
            </button>
          ))}
        </div>

        {error && (
          <div className="bg-smoke border border-smoke text-silver rounded-xl p-4 text-sm mb-6">
            {error}
          </div>
        )}

        {loading ? (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
            {Array.from({ length: 8 }).map((_, i) => (
              <div key={i} className="h-44 bg-smoke rounded-xl animate-pulse" />
            ))}
          </div>
        ) : visiveis.length === 0 ? (
          <div className="bg-charcoal rounded-xl border border-smoke p-8 text-center">
            <div className="text-4xl mb-2" aria-hidden>
              🫂
            </div>
            <p className="text-silver">
              {busca
                ? "Nenhuma comunidade encontrada com essa busca."
                : items.length === 0
                  ? `Ainda não há comunidades por aqui — ${
                      user?.verified
                        ? "crie a primeira!"
                        : "seja a primeira pessoa a criar uma."
                    }`
                  : "Nenhuma comunidade neste eixo."}
            </p>
            {!busca && items.length === 0 && user?.verified && (
              <button
                onClick={() => setShowCreate(true)}
                className="mt-4 inline-block bg-community text-ink font-semibold px-5 py-2.5 rounded-xl"
              >
                Criar comunidade
              </button>
            )}
          </div>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
            {visiveis.map((c) => (
              <CommunityCard key={c.Id} c={c} />
            ))}
          </div>
        )}
      </section>

      {showCreate && (
        <CreateCommunityModal
          onClose={() => setShowCreate(false)}
          onCreated={(id) => navigate(`/community/${id}`)}
        />
      )}
    </div>
  );
}

function CreateCommunityModal({
  onClose,
  onCreated,
}: {
  onClose: () => void;
  onCreated: (id: string) => void;
}) {
  const [nome, setNome] = useState("");
  const [descricao, setDescricao] = useState("");
  const [eixo, setEixo] = useState<CommunityAxis>("Geo");
  const [visibilidade, setVisibilidade] = useState<CommunityVisibility>("Open");
  const [password, setPassword] = useState("");
  const [bairro, setBairro] = useState("");
  const [cidade, setCidade] = useState("");
  const [estado, setEstado] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    if (!nome.trim()) return setError("Dê um nome à comunidade.");
    if (visibilidade === "Private" && !password.trim())
      return setError("Comunidades privadas precisam de uma senha de acesso.");

    const body: CreateCommunityBody = {
      Name: nome.trim(),
      Description: descricao.trim(),
      Type: "User",
      Axis: eixo,
      Visibility: visibilidade,
      Password: visibilidade === "Private" ? password : undefined,
      Neighborhood: bairro.trim() || undefined,
      City: cidade.trim() || undefined,
      State: estado.trim() || undefined,
    };

    setLoading(true);
    try {
      const id = await api.createCommunity(body);
      onCreated(String(id));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Falha ao criar comunidade.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div
      className="fixed inset-0 z-50 bg-ink/80 backdrop-blur-sm flex items-start sm:items-center justify-center p-4 overflow-y-auto"
      onClick={onClose}
    >
      <div
        className="bg-charcoal border border-smoke rounded-2xl w-full max-w-lg my-8"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-center justify-between p-4 border-b border-smoke">
          <h2 className="font-bold text-cream">Criar comunidade</h2>
          <button
            onClick={onClose}
            className="text-silver hover:text-cream"
            aria-label="Fechar"
          >
            ✕
          </button>
        </div>
        <form onSubmit={submit} className="p-4 space-y-4">
          <label className={labelCls}>
            <span className={labelTxtCls}>Nome</span>
            <input
              value={nome}
              onChange={(e) => setNome(e.target.value)}
              className={inputCls}
              placeholder="Ex.: Vizinhos do Bairro Centro"
            />
          </label>

          <label className={labelCls}>
            <span className={labelTxtCls}>Descrição</span>
            <textarea
              value={descricao}
              onChange={(e) => setDescricao(e.target.value)}
              rows={3}
              className={inputCls}
              placeholder="Do que se trata esta comunidade?"
            />
          </label>

          <div className="grid grid-cols-2 gap-3">
            <label className={labelCls}>
              <span className={labelTxtCls}>Eixo</span>
              <select
                value={eixo}
                onChange={(e) => setEixo(e.target.value as CommunityAxis)}
                className={inputCls}
              >
                {EIXOS.map((x) => (
                  <option key={x} value={x}>
                    {EIXO_EMOJI[x]} {EIXO_LABEL[x]}
                  </option>
                ))}
              </select>
            </label>
            <label className={labelCls}>
              <span className={labelTxtCls}>Visibilidade</span>
              <select
                value={visibilidade}
                onChange={(e) =>
                  setVisibilidade(e.target.value as CommunityVisibility)
                }
                className={inputCls}
              >
                {VISIBILIDADES.map((v) => (
                  <option key={v} value={v}>
                    {VISIBILIDADE_LABEL[v]}
                  </option>
                ))}
              </select>
            </label>
          </div>

          {visibilidade === "Private" && (
            <label className={labelCls}>
              <span className={labelTxtCls}>Senha de acesso</span>
              <input
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className={inputCls}
                placeholder="Compartilhe com quem convidar"
              />
            </label>
          )}

          <details className="bg-smoke rounded-xl p-3">
            <summary className={`${labelTxtCls} cursor-pointer`}>
              Localização (opcional)
            </summary>
            <div className="mt-3 grid grid-cols-1 sm:grid-cols-3 gap-3">
              <label className={labelCls}>
                <span className={labelTxtCls}>Bairro</span>
                <input
                  value={bairro}
                  onChange={(e) => setBairro(e.target.value)}
                  className={inputCls}
                />
              </label>
              <label className={labelCls}>
                <span className={labelTxtCls}>Cidade</span>
                <input
                  value={cidade}
                  onChange={(e) => setCidade(e.target.value)}
                  className={inputCls}
                />
              </label>
              <label className={labelCls}>
                <span className={labelTxtCls}>Estado</span>
                <input
                  value={estado}
                  onChange={(e) => setEstado(e.target.value)}
                  className={inputCls}
                />
              </label>
            </div>
          </details>

          {error && <p className="text-rosa text-sm">{error}</p>}

          <div className="flex items-center gap-3 pt-1">
            <button
              disabled={loading}
              className="flex-1 bg-brand text-ink font-semibold px-6 py-3 rounded-xl disabled:opacity-60"
            >
              {loading ? "Criando…" : "Criar comunidade"}
            </button>
            <button
              type="button"
              onClick={onClose}
              className="text-silver hover:text-cream px-4 py-3"
            >
              Cancelar
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
