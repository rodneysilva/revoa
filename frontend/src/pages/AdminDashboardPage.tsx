import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { ApiError, api } from "../api/client";
import type {
  AdminParameter,
  DemurragePreview,
  DemurrageRun,
  ParameterType,
  SeedCatalogResult,
} from "../api/types";
import { useAuth } from "../auth/AuthContext";
import { isAdminUser } from "../lib/admin";

type FormValue = number | boolean | string;

function formatDateTime(iso?: string): string {
  if (!iso) return "—";
  try {
    return new Date(iso).toLocaleString("pt-BR", {
      day: "2-digit",
      month: "2-digit",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  } catch {
    return iso;
  }
}

function rvm(raw: number): string {
  return raw.toLocaleString("pt-BR", { maximumFractionDigits: 4 });
}

export function AdminDashboardPage() {
  const { user } = useAuth();

  // Parâmetros.
  const [params, setParams] = useState<AdminParameter[]>([]);
  const [form, setForm] = useState<Record<string, FormValue>>({});
  const [loadingParams, setLoadingParams] = useState(true);
  const [paramError, setParamError] = useState<string | null>(null);
  const [savingKey, setSavingKey] = useState<string | null>(null);
  const [savingAll, setSavingAll] = useState(false);
  const [paramMsg, setParamMsg] = useState<string | null>(null);

  // Pricing.
  const [pricingBusy, setPricingBusy] = useState(false);
  const [pricingMsg, setPricingMsg] = useState<string | null>(null);

  // Demurrage.
  const [demoBusy, setDemoBusy] = useState<"preview" | "run" | null>(null);
  const [demoMsg, setDemoMsg] = useState<string | null>(null);
  const [demoPreview, setDemoPreview] = useState<DemurragePreview | null>(null);
  const [demoRuns, setDemoRuns] = useState<DemurrageRun[]>([]);

  // Seed.
  const [seedBusy, setSeedBusy] = useState(false);
  const [seedResult, setSeedResult] = useState<SeedCatalogResult | null>(null);
  const [seedError, setSeedError] = useState<string | null>(null);

  const loadParams = useCallback(async () => {
    setLoadingParams(true);
    setParamError(null);
    try {
      const data = await api.adminParameters();
      setParams(data);
      const next: Record<string, FormValue> = {};
      for (const p of data) next[p.Key] = p.Value;
      setForm(next);
    } catch (e) {
      setParamError(e instanceof ApiError ? e.message : "Falha ao carregar parâmetros.");
    } finally {
      setLoadingParams(false);
    }
  }, []);

  const loadRuns = useCallback(async () => {
    try {
      setDemoRuns(await api.demurrageRuns(10));
    } catch {
      /* histórico é informativo — ignora erros */
    }
  }, []);

  useEffect(() => {
    loadParams();
    loadRuns();
  }, [loadParams, loadRuns]);

  if (!isAdminUser(user)) {
    return (
      <div className="app-container text-center py-16">
        <h1 className="text-2xl font-bold text-cream mb-3">Acesso restrito a administradores</h1>
        <p className="text-silver mb-4">Esta área é exclusiva da administração da plataforma.</p>
        <Link to="/feed" className="text-esmeralda hover:underline">
          ← Voltar ao feed
        </Link>
      </div>
    );
  }

  async function saveOne(p: AdminParameter) {
    const value = form[p.Key];
    if (value === undefined) return;
    setSavingKey(p.Key);
    setParamError(null);
    setParamMsg(null);
    try {
      const toSend = p.Type === "bool" ? Boolean(value) : Number(value);
      await api.setAdminParameter(p.Key, toSend);
      await loadParams();
      setParamMsg("Parâmetro atualizado.");
    } catch (e) {
      setParamError(e instanceof ApiError ? e.message : "Não foi possível atualizar o parâmetro.");
    } finally {
      setSavingKey(null);
    }
  }

  async function saveAll() {
    setSavingAll(true);
    setParamError(null);
    setParamMsg(null);
    try {
      for (const p of params) {
        const value = form[p.Key];
        if (value === undefined) continue;
        const toSend = p.Type === "bool" ? Boolean(value) : Number(value);
        await api.setAdminParameter(p.Key, toSend);
      }
      await loadParams();
      setParamMsg("Parâmetros salvos.");
    } catch (e) {
      setParamError(e instanceof ApiError ? e.message : "Falha ao salvar um ou mais parâmetros.");
    } finally {
      setSavingAll(false);
    }
  }

  async function refreshPricing() {
    setPricingBusy(true);
    setPricingMsg(null);
    try {
      const res = await api.refreshPricing();
      setPricingMsg(`${res.updated} categoria(s) atualizada(s).`);
    } catch (e) {
      setPricingMsg(e instanceof ApiError ? e.message : "Falha ao atualizar preços.");
    } finally {
      setPricingBusy(false);
    }
  }

  async function runDemurragePreview() {
    setDemoBusy("preview");
    setDemoMsg(null);
    try {
      setDemoPreview(await api.demurragePreview());
    } catch (e) {
      setDemoMsg(e instanceof ApiError ? e.message : "Falha na pré-visualização.");
    } finally {
      setDemoBusy(null);
    }
  }

  async function runDemurrage() {
    if (!confirm("Executar o demurrage agora? Isto queima RVM on-chain das carteiras acima do piso.")) {
      return;
    }
    setDemoBusy("run");
    setDemoMsg(null);
    try {
      const run = await api.demurrageRun();
      setDemoMsg(
        `Executado: ${run.AccountsAffected} carteira(s) afetada(s), ${rvm(run.TotalBurnedRvm)} RVM queimados.`
      );
      setDemoPreview(null);
      await loadRuns();
    } catch (e) {
      setDemoMsg(e instanceof ApiError ? e.message : "Falha ao executar o demurrage.");
    } finally {
      setDemoBusy(null);
    }
  }

  async function seed() {
    if (!confirm("Re-semear o catálogo de demonstração? Substitui os anúncios [Demo].")) return;
    setSeedBusy(true);
    setSeedResult(null);
    setSeedError(null);
    try {
      setSeedResult(await api.seedCatalog());
    } catch (e) {
      setSeedError(e instanceof ApiError ? e.message : "Falha ao semear o catálogo.");
    } finally {
      setSeedBusy(false);
    }
  }

  return (
    <div className="app-container py-6">
      <h1 className="text-2xl font-bold text-cream mb-1">Administração</h1>
      <p className="text-silver text-sm mb-6">
        Parâmetros runtime (valem imediatamente, sem reiniciar) + atalhos para as áreas administrativas.
      </p>

      {/* Parâmetros do sistema */}
      <section className="bg-charcoal rounded-2xl border border-smoke p-4 sm:p-5 mb-6">
        <div className="flex items-center justify-between flex-wrap gap-2 mb-4">
          <h2 className="text-cream font-semibold">Parâmetros do sistema</h2>
          <button
            type="button"
            onClick={saveAll}
            disabled={savingAll || loadingParams}
            className="bg-brand text-ink font-semibold px-4 py-1.5 rounded-lg text-sm disabled:opacity-60"
          >
            {savingAll ? "Salvando…" : "Salvar todos"}
          </button>
        </div>

        {paramMsg && <p className="text-esmeralda text-sm mb-3">{paramMsg}</p>}
        {paramError && <p className="text-rosa text-sm mb-3">{paramError}</p>}

        {loadingParams ? (
          <p className="text-silver">Carregando…</p>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            {params.map((p) => (
              <ParamRow
                key={p.Key}
                param={p}
                value={form[p.Key]}
                onChange={(v) => setForm((f) => ({ ...f, [p.Key]: v }))}
                onSave={() => saveOne(p)}
                saving={savingKey === p.Key}
              />
            ))}
          </div>
        )}
      </section>

      {/* Áreas administrativas */}
      <h2 className="text-cream font-semibold mb-3">Áreas</h2>
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
        <Card title="Cupons">
          <p className="text-silver text-sm mb-3">Criar, revogar e acompanhar cupons on-chain.</p>
          <Link to="/admin/coupons" className="text-esmeralda text-sm hover:underline">
            Gerenciar cupons →
          </Link>
        </Card>

        <Card title="Denúncias">
          <p className="text-silver text-sm mb-3">Moderar denúncias (arquivar, avisar, banir).</p>
          <Link to="/admin/reports" className="text-esmeralda text-sm hover:underline">
            Gerenciar denúncias →
          </Link>
        </Card>

        <Card title="Pricing Intelligence">
          <p className="text-silver text-sm mb-3">
            Recalcular referências de preço justo por categoria.
          </p>
          <button
            type="button"
            onClick={refreshPricing}
            disabled={pricingBusy}
            className="bg-brand text-ink font-semibold px-4 py-1.5 rounded-lg text-sm disabled:opacity-60"
          >
            {pricingBusy ? "Atualizando…" : "Atualizar preços"}
          </button>
          {pricingMsg && <p className="text-silver text-xs mt-2">{pricingMsg}</p>}
          <Link to="/explore" className="block text-esmeralda text-sm hover:underline mt-2">
            Ver anúncios →
          </Link>
        </Card>

        <Card title="Demurrage">
          <p className="text-silver text-sm mb-3">Queima periódica de uma % do RVM ocioso.</p>
          <div className="flex flex-wrap gap-2 mb-2">
            <button
              type="button"
              onClick={runDemurragePreview}
              disabled={demoBusy !== null}
              className="bg-smoke text-cream font-semibold px-3 py-1.5 rounded-lg text-sm disabled:opacity-60"
            >
              {demoBusy === "preview" ? "Calculando…" : "Preview"}
            </button>
            <button
              type="button"
              onClick={runDemurrage}
              disabled={demoBusy !== null}
              className="bg-rosa/20 text-rosa font-semibold px-3 py-1.5 rounded-lg text-sm disabled:opacity-60"
            >
              {demoBusy === "run" ? "Executando…" : "Executar"}
            </button>
          </div>
          {demoMsg && <p className="text-silver text-xs mb-2">{demoMsg}</p>}
          {demoPreview && (
            <p className="text-xs text-cream mb-2">
              {demoPreview.AccountsAffected} afetada(s) · {demoPreview.Skipped} isenta(s) ·{" "}
              {rvm(demoPreview.TotalBurnedRvm)} RVM (taxa {demoPreview.RateBps} bps, piso{" "}
              {demoPreview.FloorRvm}).
            </p>
          )}
          {demoRuns.length > 0 && (
            <details className="text-xs text-silver">
              <summary className="cursor-pointer">Histórico ({demoRuns.length})</summary>
              <ul className="mt-1 space-y-1">
                {demoRuns.map((r) => (
                  <li key={r.Id}>
                    {formatDateTime(r.RunAt)} — {r.AccountsAffected} afetada(s), {rvm(r.TotalBurnedRvm)}{" "}
                    RVM · {r.ExecutedBy}
                  </li>
                ))}
              </ul>
            </details>
          )}
        </Card>

        {import.meta.env.DEV && (
          <Card title="Catálogo demo">
            <p className="text-silver text-sm mb-3">
              Re-semear produtos e serviços de demonstração (ambiente de dev; endpoint dev-only).
            </p>
            <button
              type="button"
              onClick={seed}
              disabled={seedBusy}
              className="bg-brand text-ink font-semibold px-4 py-1.5 rounded-lg text-sm disabled:opacity-60"
            >
              {seedBusy ? "Semeando…" : "Re-semear catálogo"}
            </button>
            {seedResult && (
              <p className="text-silver text-xs mt-2">
                {seedResult.total} anúncio(s) · {seedResult.produtos} produtos · {seedResult.servicos}{" "}
                serviços · {seedResult.categorias} categoria(s).
              </p>
            )}
            {seedError && <p className="text-rosa text-xs mt-2">{seedError}</p>}
          </Card>
        )}
      </div>
    </div>
  );
}

function Card({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="bg-charcoal rounded-2xl border border-smoke p-4 sm:p-5">
      <h3 className="text-cream font-semibold mb-2">{title}</h3>
      {children}
    </div>
  );
}

function ParamRow({
  param,
  value,
  onChange,
  onSave,
  saving,
}: {
  param: AdminParameter;
  value: FormValue | undefined;
  onChange: (v: FormValue) => void;
  onSave: () => void;
  saving: boolean;
}) {
  return (
    <div className="bg-smoke/60 rounded-xl border border-smoke p-3">
      <label className="block">
        <span className="text-sm text-cream font-medium">{param.Label}</span>
        <span className="block text-[11px] text-silver mb-2">{param.Key}</span>
        {renderInput(param.Type, value, onChange)}
      </label>
      <button
        type="button"
        onClick={onSave}
        disabled={saving}
        className="mt-2 bg-brand text-ink font-semibold px-3 py-1 rounded-lg text-xs disabled:opacity-60"
      >
        {saving ? "Salvando…" : "Salvar"}
      </button>
    </div>
  );
}

function renderInput(
  type: ParameterType,
  value: FormValue | undefined,
  onChange: (v: FormValue) => void
) {
  const base =
    "mt-1 w-full bg-ink text-cream rounded-lg border border-smoke focus:border-esmeralda px-3 py-2 outline-none text-sm";

  if (type === "bool") {
    return (
      <input
        type="checkbox"
        checked={Boolean(value)}
        onChange={(e) => onChange(e.target.checked)}
        className="mt-1 h-5 w-5 accent-[#10b981]"
        aria-label="ativo"
      />
    );
  }

  const step = type === "decimal" ? "0.01" : "1";
  const min = "0";
  return (
    <input
      type="number"
      step={step}
      min={min}
      value={value === undefined ? "" : String(value)}
      onChange={(e) => onChange(Number(e.target.value))}
      className={base}
    />
  );
}
